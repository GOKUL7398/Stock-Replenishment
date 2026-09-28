using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Domain.Entities;

public class ReplenishmentRequest
{
    public int Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public int StockLocationId { get; set; }

    public StockLocation StockLocation { get; set; } = null!;

    public RequestPriority Priority { get; set; }

    public ReplenishmentStatus Status { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedBy { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? FulfilledAt { get; set; }

    public ICollection<ReplenishmentLine> Lines { get; set; } = new List<ReplenishmentLine>();

    public StockValidation? StockValidation { get; set; }
}
