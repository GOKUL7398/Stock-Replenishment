namespace StockReplenishment.Application.Interfaces;

/// <summary>In-process queue that decouples request submission from the slow external stock check.</summary>
public interface IStockValidationQueue
{
    ValueTask QueueAsync(int requestId);

    ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
}
