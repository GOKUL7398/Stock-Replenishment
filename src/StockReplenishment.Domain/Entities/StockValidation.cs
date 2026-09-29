using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Domain.Entities;

public class StockValidation
{
    public int Id { get; set; }

    public int ReplenishmentRequestId { get; set; }

    public ReplenishmentRequest ReplenishmentRequest { get; set; } = null!;

    public StockValidationStatus Status { get; set; }

    public bool? IsAvailable { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
