using HomeInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.NormalizedName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Category).HasMaxLength(100);
        builder.Property(i => i.Barcode).HasMaxLength(64);
        builder.Property(i => i.PhotoUrl).HasMaxLength(2048);
        builder.Property(i => i.Unit).HasMaxLength(32);
        builder.Property(i => i.TrackingType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(i => i.HouseholdId);

        // Normalized name unique within each household.
        builder.HasIndex(i => new { i.HouseholdId, i.NormalizedName })
            .IsUnique()
            .HasDatabaseName("ix_items_household_normalized_name");

        // GIN trigram index for fuzzy Spanish-language search over the normalized name.
        builder.HasIndex(i => i.NormalizedName)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_items_normalized_name_trgm");
    }
}
