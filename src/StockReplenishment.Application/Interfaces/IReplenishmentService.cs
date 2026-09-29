using StockReplenishment.Application.DTOs;
using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Application.Interfaces;

public interface IReplenishmentService
{
    Task<PaginatedResultDto<ReplenishmentRequestDto>> GetRequestsAsync(
        ReplenishmentStatus? status,
        RequestPriority? priority,
        int? locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ReplenishmentRequestDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StockLocationDto>> GetLocationsAsync(CancellationToken cancellationToken);

    Task<ReplenishmentRequestDto> CreateAsync(CreateReplenishmentRequestDto dto, CancellationToken cancellationToken);

    Task<ReplenishmentRequestDto> UpdateAsync(int id, UpdateReplenishmentRequestDto dto, CancellationToken cancellationToken);

    Task<ReplenishmentRequestDto> SubmitAsync(int id, CancellationToken cancellationToken);

    Task<ReplenishmentRequestDto> ApproveAsync(int id, ApproveReplenishmentRequestDto dto, CancellationToken cancellationToken);

    Task<ReplenishmentRequestDto> RejectAsync(int id, RejectReplenishmentRequestDto dto, CancellationToken cancellationToken);

    Task<ReplenishmentRequestDto> FulfillAsync(int id, FulfillReplenishmentRequestDto dto, CancellationToken cancellationToken);

    Task<StockValidationResultDto> GetValidationAsync(int id, CancellationToken cancellationToken);

    Task ValidateStockAsync(int id, CancellationToken cancellationToken);
}
