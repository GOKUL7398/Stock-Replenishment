using StockReplenishment.Application.Interfaces;
using StockReplenishment.Domain.Entities;

namespace StockReplenishment.Infrastructure.Services;

/// <summary>Simulates a slow external stock-availability system (3-7s latency).</summary>
public class SlowStockAvailabilityService : IStockAvailabilityService
{
    private readonly Random _random = new();

    public async Task<StockAvailabilityResult> CheckAvailabilityAsync(
        ReplenishmentRequest request,
        CancellationToken cancellationToken)
    {
        var delay = _random.Next(3000, 7000);

        await Task.Delay(delay, cancellationToken);

        var available = request.Lines.All(x => x.RequestedQuantity <= 500);

        return new StockAvailabilityResult
        {
            IsAvailable = available,
            Message = available
                ? "All requested materials are available."
                : "One or more requested quantities are unavailable."
        };
    }
}
