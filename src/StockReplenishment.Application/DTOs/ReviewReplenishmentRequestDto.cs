namespace StockReplenishment.Application.DTOs;

public class RejectReplenishmentRequestDto
{
    public string Reason { get; set; } = string.Empty;

    public string ReviewedBy { get; set; } = string.Empty;
}

public class ApproveReplenishmentRequestDto
{
    public string ReviewedBy { get; set; } = string.Empty;
}

public class FulfilledLineDto
{
    public int LineId { get; set; }

    public decimal FulfilledQuantity { get; set; }
}

public class FulfillReplenishmentRequestDto
{
    public List<FulfilledLineDto> Lines { get; set; } = [];

    public string FulfilledBy { get; set; } = string.Empty;
}
