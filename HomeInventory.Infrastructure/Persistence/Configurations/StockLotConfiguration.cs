using HomeInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory.Infrastructure.Persistence.Configurations;

public class StockLotConfiguration : IEntityTypeConfiguration<StockLot>
{
    public void Configure(EntityTypeBuilder<StockLot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Quantity).HasPrecision(18, 3);
        // ExpirationDate / AcquiredDate (DateOnly?) → Postgres 'date' column (Npgsql).

        builder.HasIndex(s => s.HouseholdId);
        builder.HasIndex(s => s.ItemId);
        builder.HasIndex(s => s.LocationId);

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(s => s.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
