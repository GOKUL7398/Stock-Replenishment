namespace StockReplenishment.Application.Interfaces;

public class StockAvailabilityResult
{
    public bool IsAvailable { get; set; }

    public string Message { get; set; } = string.Empty;
}

public interface IStockAvailabilityService
{
    Task<StockAvailabilityResult> CheckAvailabilityAsync(
        Domain.Entities.ReplenishmentRequest request,
        CancellationToken cancellationToken);
}
