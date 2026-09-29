using System.Threading.Channels;
using StockReplenishment.Application.Interfaces;

namespace StockReplenishment.Infrastructure.Services;

public class StockValidationQueue : IStockValidationQueue
{
    private readonly Channel<int> _queue = Channel.CreateUnbounded<int>();

    public async ValueTask QueueAsync(int requestId)
    {
        await _queue.Writer.WriteAsync(requestId);
    }

    public async ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
