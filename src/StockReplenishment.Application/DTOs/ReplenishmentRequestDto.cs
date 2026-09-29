using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Application.DTOs;

public class ReplenishmentLineDto
{
    public int Id { get; set; }

    public string ArticleNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal RequestedQuantity { get; set; }

    public decimal? FulfilledQuantity { get; set; }
}

public class ReplenishmentRequestDto
{
    public int Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public int StockLocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public RequestPriority Priority { get; set; }

    public ReplenishmentStatus Status { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedBy { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? FulfilledAt { get; set; }

    public List<ReplenishmentLineDto> Lines { get; set; } = [];
}

public class StockLocationDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
