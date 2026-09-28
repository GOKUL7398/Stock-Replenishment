using Microsoft.EntityFrameworkCore;
using StockReplenishment.Domain.Entities;

namespace StockReplenishment.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ReplenishmentRequest> ReplenishmentRequests => Set<ReplenishmentRequest>();

    public DbSet<ReplenishmentLine> ReplenishmentLines => Set<ReplenishmentLine>();

    public DbSet<StockLocation> StockLocations => Set<StockLocation>();

    public DbSet<StockValidation> StockValidations => Set<StockValidation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReplenishmentRequest>()
            .HasMany(x => x.Lines)
            .WithOne(x => x.ReplenishmentRequest)
            .HasForeignKey(x => x.ReplenishmentRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReplenishmentRequest>()
            .HasOne(x => x.StockLocation)
            .WithMany(x => x.Requests)
            .HasForeignKey(x => x.StockLocationId);

        modelBuilder.Entity<ReplenishmentRequest>()
            .HasOne(x => x.StockValidation)
            .WithOne(x => x.ReplenishmentRequest)
            .HasForeignKey<StockValidation>(x => x.ReplenishmentRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReplenishmentRequest>()
            .Property(x => x.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<ReplenishmentRequest>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<StockValidation>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ReplenishmentRequest>()
            .HasIndex(x => x.RequestNumber)
            .IsUnique();
    }
}
