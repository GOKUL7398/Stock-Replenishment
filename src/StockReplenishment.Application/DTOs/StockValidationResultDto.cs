using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Application.DTOs;

public class StockValidationResultDto
{
    public StockValidationStatus Status { get; set; }

    public bool? IsAvailable { get; set; }

    public string? Message { get; set; }

    public DateTime? CompletedAt { get; set; }
}
