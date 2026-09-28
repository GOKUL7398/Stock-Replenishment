using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Application.DTOs;

public class CreateReplenishmentLineDto
{
    public string ArticleNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal RequestedQuantity { get; set; }
}

public class CreateReplenishmentRequestDto
{
    public int StockLocationId { get; set; }

    public RequestPriority Priority { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public List<CreateReplenishmentLineDto> Lines { get; set; } = [];
}

public class UpdateReplenishmentRequestDto
{
    public int StockLocationId { get; set; }

    public RequestPriority Priority { get; set; }

    public List<CreateReplenishmentLineDto> Lines { get; set; } = [];
}
