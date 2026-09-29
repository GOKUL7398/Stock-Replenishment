using Microsoft.EntityFrameworkCore;
using StockReplenishment.Domain.Entities;
using StockReplenishment.Domain.Enums;

namespace StockReplenishment.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.StockLocations.AnyAsync())
            return;

        var locations = new[]
        {
            new StockLocation { Id = 1, Code = "LINE-01", Name = "Assembly Line 01" },
            new StockLocation { Id = 2, Code = "LINE-02", Name = "Assembly Line 02" },
            new StockLocation { Id = 3, Code = "WH-A", Name = "Warehouse A" }
        };

        context.StockLocations.AddRange(locations);

        var requests = new[]
        {
            new ReplenishmentRequest
            {
                Id = 1,
                RequestNumber = "REQ-0001",
                StockLocationId = 1,
                Priority = RequestPriority.Urgent,
                Status = ReplenishmentStatus.Submitted,
                RequestedBy = "worker1",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                SubmittedAt = DateTime.UtcNow.AddHours(-2),
                Lines =
                {
                    new ReplenishmentLine
                    {
                        ArticleNumber = "MAT-1001",
                        Description = "Steel Bolt M10",
                        RequestedQuantity = 100
                    }
                },
                // every Submitted request must have a validation row, mirroring what SubmitAsync creates
                StockValidation = new StockValidation
                {
                    Status = StockValidationStatus.Completed,
                    IsAvailable = true,
                    Message = "All requested materials are available.",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    CompletedAt = DateTime.UtcNow.AddHours(-2).AddSeconds(5)
                }
            },
            new ReplenishmentRequest
            {
                Id = 2,
                RequestNumber = "REQ-0002",
                StockLocationId = 2,
                Priority = RequestPriority.Normal,
                Status = ReplenishmentStatus.Approved,
                RequestedBy = "worker2",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                SubmittedAt = DateTime.UtcNow.AddHours(-20),
                ReviewedAt = DateTime.UtcNow.AddHours(-18),
                ReviewedBy = "supervisor",
                Lines =
                {
                    new ReplenishmentLine
                    {
                        ArticleNumber = "MAT-2001",
                        Description = "Plastic Housing",
                        RequestedQuantity = 50
                    }
                }
            },
            new ReplenishmentRequest
            {
                Id = 3,
                RequestNumber = "REQ-0003",
                StockLocationId = 1,
                Priority = RequestPriority.Low,
                Status = ReplenishmentStatus.Draft,
                RequestedBy = "worker3",
                CreatedAt = DateTime.UtcNow,
                Lines =
                {
                    new ReplenishmentLine
                    {
                        ArticleNumber = "MAT-3001",
                        Description = "Copper Washer",
                        RequestedQuantity = 200
                    }
                }
            }
        };

        context.ReplenishmentRequests.AddRange(requests);

        await context.SaveChangesAsync();
    }
}
