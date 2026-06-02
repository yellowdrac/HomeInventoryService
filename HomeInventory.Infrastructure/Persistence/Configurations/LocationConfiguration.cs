using HomeInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory.Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);
        builder.Property(l => l.QrSlug).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(l => l.QrSlug).IsUnique();
        builder.HasIndex(l => l.HouseholdId);

        // Self-referencing FK: one location hangs off another. Restrict prevents
        // deleting a node that has children via cascade.
        builder.HasOne(l => l.Parent)
            .WithMany(l => l.Children)
            .HasForeignKey(l => l.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
