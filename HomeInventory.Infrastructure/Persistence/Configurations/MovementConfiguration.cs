using HomeInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory.Infrastructure.Persistence.Configurations;

public class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
    public void Configure(EntityTypeBuilder<Movement> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Quantity).HasPrecision(18, 3);
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Reason).HasMaxLength(500);

        builder.HasIndex(m => m.HouseholdId);
        builder.HasIndex(m => m.ItemId);
        builder.HasIndex(m => m.OccurredAt);

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(m => m.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(m => m.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(m => m.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
