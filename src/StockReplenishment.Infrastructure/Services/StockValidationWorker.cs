using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockReplenishment.Application.Interfaces;

namespace StockReplenishment.Infrastructure.Services;

/// <summary>Drains the validation queue so stock checks run off the request/response cycle.</summary>
public class StockValidationWorker(
    IServiceScopeFactory scopeFactory,
    IStockValidationQueue queue,
    ILogger<StockValidationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var requestId = await queue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();

                var service = scope.ServiceProvider.GetRequiredService<IReplenishmentService>();

                await service.ValidateStockAsync(requestId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stock validation failed for request {RequestId}", requestId);
            }
        }
    }
}
