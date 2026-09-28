using Microsoft.AspNetCore.Mvc;
using StockReplenishment.Application.DTOs;
using StockReplenishment.Application.Exceptions;
using StockReplenishment.Application.Interfaces;
using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Web.Controllers;

[ApiController]
[Route("api/replenishment-requests")]
public class ReplenishmentRequestsController(IReplenishmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResultDto<ReplenishmentRequestDto>>> GetRequests(
        [FromQuery] ReplenishmentStatus? status,
        [FromQuery] RequestPriority? priority,
        [FromQuery] int? locationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetRequestsAsync(status, priority, locationId, page, pageSize, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReplenishmentRequestDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);

        return Ok(result);
    }

    [HttpGet("locations")]
    public async Task<ActionResult<IReadOnlyCollection<StockLocationDto>>> GetLocations(CancellationToken cancellationToken)
    {
        var result = await service.GetLocationsAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReplenishmentRequestDto>> Create(
        [FromBody] CreateReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(dto, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReplenishmentRequestDto>> Update(
        int id,
        [FromBody] UpdateReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, dto, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        var result = await service.SubmitAsync(id, cancellationToken);

        return Accepted(new
        {
            result.Id,
            result.Status,
            message = "Request submitted. Stock validation is running."
        });
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<ReplenishmentRequestDto>> Approve(
        int id,
        [FromBody] ApproveReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await service.ApproveAsync(id, dto, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<ReplenishmentRequestDto>> Reject(
        int id,
        [FromBody] RejectReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await service.RejectAsync(id, dto, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:int}/fulfill")]
    public async Task<ActionResult<ReplenishmentRequestDto>> Fulfill(
        int id,
        [FromBody] FulfillReplenishmentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await service.FulfillAsync(id, dto, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}/validation")]
    public async Task<ActionResult<StockValidationResultDto>> GetValidation(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetValidationAsync(id, cancellationToken);

        return Ok(result);
    }
}
