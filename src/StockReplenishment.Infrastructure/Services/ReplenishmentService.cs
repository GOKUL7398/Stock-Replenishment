using Microsoft.EntityFrameworkCore;
using StockReplenishment.Application.DTOs;
using StockReplenishment.Application.Exceptions;
using StockReplenishment.Application.Interfaces;
using StockReplenishment.Domain.Entities;
using StockReplenishment.Domain.Enums;
using StockReplenishment.Infrastructure.Data;

namespace StockReplenishment.Infrastructure.Services;

public class ReplenishmentService(
    AppDbContext context,
    IStockValidationQueue validationQueue,
    IStockAvailabilityService stockAvailabilityService) : IReplenishmentService
{
    public async Task<PaginatedResultDto<ReplenishmentRequestDto>> GetRequestsAsync(
        ReplenishmentStatus? status,
        RequestPriority? priority,
        int? locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1)
            throw new BusinessRuleException("Page must be greater than zero.");

        if (pageSize is < 1 or > 100)
            throw new BusinessRuleException("Page size must be between 1 and 100.");

        var query = context.ReplenishmentRequests
            .AsNoTracking()
            .Include(x => x.StockLocation)
            .Include(x => x.Lines)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(x => x.Priority == priority.Value);

        if (locationId.HasValue)
            query = query.Where(x => x.StockLocationId == locationId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResultDto<ReplenishmentRequestDto>
        {
            Items = requests.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ReplenishmentRequestDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var request = await FindRequestAsync(id, cancellationToken);

        return ToDto(request);
    }

    public async Task<IReadOnlyCollection<StockLocationDto>> GetLocationsAsync(CancellationToken cancellationToken)
    {
        return await context.StockLocations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new StockLocationDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<ReplenishmentRequestDto> CreateAsync(
        CreateReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        ValidateLines(dto.Lines);

        if (string.IsNullOrWhiteSpace(dto.RequestedBy))
            throw new BusinessRuleException("RequestedBy is required.");

        var locationExists = await context.StockLocations.AnyAsync(x => x.Id == dto.StockLocationId, cancellationToken);

        if (!locationExists)
            throw new NotFoundException($"Stock location {dto.StockLocationId} was not found.");

        var request = new ReplenishmentRequest
        {
            RequestNumber = await GenerateRequestNumberAsync(cancellationToken),
            StockLocationId = dto.StockLocationId,
            Priority = dto.Priority,
            Status = ReplenishmentStatus.Draft,
            RequestedBy = dto.RequestedBy,
            CreatedAt = DateTime.UtcNow,
            Lines = dto.Lines.Select(x => new ReplenishmentLine
            {
                ArticleNumber = x.ArticleNumber,
                Description = x.Description,
                RequestedQuantity = x.RequestedQuantity
            }).ToList()
        };

        context.ReplenishmentRequests.Add(request);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(request.Id, cancellationToken);
    }

    public async Task<ReplenishmentRequestDto> UpdateAsync(
        int id,
        UpdateReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        ValidateLines(dto.Lines);

        var request = await context.ReplenishmentRequests
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Replenishment request {id} was not found.");

        if (request.Status != ReplenishmentStatus.Draft)
            throw new BusinessRuleException("Only draft requests can be updated.");

        var locationExists = await context.StockLocations.AnyAsync(x => x.Id == dto.StockLocationId, cancellationToken);

        if (!locationExists)
            throw new NotFoundException($"Stock location {dto.StockLocationId} was not found.");

        request.StockLocationId = dto.StockLocationId;
        request.Priority = dto.Priority;

        context.ReplenishmentLines.RemoveRange(request.Lines);
        request.Lines = dto.Lines.Select(x => new ReplenishmentLine
        {
            ArticleNumber = x.ArticleNumber,
            Description = x.Description,
            RequestedQuantity = x.RequestedQuantity
        }).ToList();

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ReplenishmentRequestDto> SubmitAsync(int id, CancellationToken cancellationToken)
    {
        var request = await context.ReplenishmentRequests
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Replenishment request {id} was not found.");

        if (request.Status != ReplenishmentStatus.Draft)
            throw new BusinessRuleException("Only draft requests can be submitted.");

        if (request.Lines.Count == 0)
            throw new BusinessRuleException("At least one material line is required.");

        request.Status = ReplenishmentStatus.Submitted;
        request.SubmittedAt = DateTime.UtcNow;

        context.StockValidations.Add(new StockValidation
        {
            ReplenishmentRequestId = request.Id,
            Status = StockValidationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);

        await validationQueue.QueueAsync(request.Id);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ReplenishmentRequestDto> ApproveAsync(
        int id,
        ApproveReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var request = await FindRequestAsync(id, cancellationToken);

        if (request.Status != ReplenishmentStatus.Submitted)
            throw new BusinessRuleException("Only submitted requests can be approved.");

        if (string.IsNullOrWhiteSpace(dto.ReviewedBy))
            throw new BusinessRuleException("ReviewedBy is required.");

        request.Status = ReplenishmentStatus.Approved;
        request.ReviewedBy = dto.ReviewedBy;
        request.ReviewedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ReplenishmentRequestDto> RejectAsync(
        int id,
        RejectReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var request = await FindRequestAsync(id, cancellationToken);

        if (request.Status != ReplenishmentStatus.Submitted)
            throw new BusinessRuleException("Only submitted requests can be rejected.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BusinessRuleException("A rejection reason is required.");

        request.Status = ReplenishmentStatus.Rejected;
        request.RejectionReason = dto.Reason;
        request.ReviewedBy = dto.ReviewedBy;
        request.ReviewedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ReplenishmentRequestDto> FulfillAsync(
        int id,
        FulfillReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var request = await context.ReplenishmentRequests
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Replenishment request {id} was not found.");

        if (request.Status != ReplenishmentStatus.Approved)
            throw new BusinessRuleException("Only approved requests can be fulfilled.");

        foreach (var lineDto in dto.Lines)
        {
            var line = request.Lines.FirstOrDefault(x => x.Id == lineDto.LineId)
                ?? throw new NotFoundException($"Line {lineDto.LineId} was not found on this request.");

            if (lineDto.FulfilledQuantity < 0)
                throw new BusinessRuleException("Fulfilled quantity cannot be negative.");

            if (lineDto.FulfilledQuantity > line.RequestedQuantity)
                throw new BusinessRuleException("Fulfilled quantity cannot exceed the requested quantity.");

            line.FulfilledQuantity = lineDto.FulfilledQuantity;
        }

        request.Status = ReplenishmentStatus.Fulfilled;
        request.FulfilledAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<StockValidationResultDto> GetValidationAsync(int id, CancellationToken cancellationToken)
    {
        var validation = await context.StockValidations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReplenishmentRequestId == id, cancellationToken)
            ?? throw new NotFoundException($"No stock validation found for request {id}.");

        return new StockValidationResultDto
        {
            Status = validation.Status,
            IsAvailable = validation.IsAvailable,
            Message = validation.Message,
            CompletedAt = validation.CompletedAt
        };
    }

    public async Task ValidateStockAsync(int id, CancellationToken cancellationToken)
    {
        var request = await context.ReplenishmentRequests
            .Include(x => x.Lines)
            .Include(x => x.StockValidation)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Replenishment request {id} was not found.");

        var validation = request.StockValidation
            ?? throw new NotFoundException($"No stock validation record found for request {id}.");

        try
        {
            var result = await stockAvailabilityService.CheckAvailabilityAsync(request, cancellationToken);

            validation.Status = StockValidationStatus.Completed;
            validation.IsAvailable = result.IsAvailable;
            validation.Message = result.Message;
            validation.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            validation.Status = StockValidationStatus.Failed;
            validation.Message = "Stock validation could not be completed.";
            validation.CompletedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<ReplenishmentRequest> FindRequestAsync(int id, CancellationToken cancellationToken)
    {
        return await context.ReplenishmentRequests
            .Include(x => x.StockLocation)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Replenishment request {id} was not found.");
    }

    private async Task<string> GenerateRequestNumberAsync(CancellationToken cancellationToken)
    {
        var count = await context.ReplenishmentRequests.CountAsync(cancellationToken);

        return $"REQ-{count + 1:D4}";
    }

    private static void ValidateLines(List<CreateReplenishmentLineDto> lines)
    {
        if (lines.Count == 0)
            throw new BusinessRuleException("At least one material line is required.");

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.ArticleNumber))
                throw new BusinessRuleException("ArticleNumber is required.");

            if (string.IsNullOrWhiteSpace(line.Description))
                throw new BusinessRuleException("Description is required.");

            if (line.RequestedQuantity <= 0)
                throw new BusinessRuleException("RequestedQuantity must be greater than zero.");
        }
    }

    private static ReplenishmentRequestDto ToDto(ReplenishmentRequest request)
    {
        return new ReplenishmentRequestDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            StockLocationId = request.StockLocationId,
            LocationName = request.StockLocation?.Name ?? string.Empty,
            Priority = request.Priority,
            Status = request.Status,
            RequestedBy = request.RequestedBy,
            CreatedAt = request.CreatedAt,
            SubmittedAt = request.SubmittedAt,
            ReviewedAt = request.ReviewedAt,
            ReviewedBy = request.ReviewedBy,
            RejectionReason = request.RejectionReason,
            FulfilledAt = request.FulfilledAt,
            Lines = request.Lines.Select(x => new ReplenishmentLineDto
            {
                Id = x.Id,
                ArticleNumber = x.ArticleNumber,
                Description = x.Description,
                RequestedQuantity = x.RequestedQuantity,
                FulfilledQuantity = x.FulfilledQuantity
            }).ToList()
        };
    }
}
