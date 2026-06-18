using HomeInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).HasMaxLength(64).IsRequired();
        builder.Property(u => u.Symbol).HasMaxLength(16).IsRequired();
        builder.Property(u => u.Category).HasMaxLength(32).IsRequired();
        builder.HasData(SeedData());
    }

    private static IEnumerable<Unit> SeedData() =>
    [
        // Count
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000001"), Name = "Unit",         Symbol = "unit",   Category = "Count",  SortOrder = 1 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000002"), Name = "Pack",         Symbol = "pack",   Category = "Count",  SortOrder = 2 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000003"), Name = "Box",          Symbol = "box",    Category = "Count",  SortOrder = 3 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000004"), Name = "Bag",          Symbol = "bag",    Category = "Count",  SortOrder = 4 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000005"), Name = "Bottle",       Symbol = "bottle", Category = "Count",  SortOrder = 5 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000006"), Name = "Can",          Symbol = "can",    Category = "Count",  SortOrder = 6 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000007"), Name = "Jar",          Symbol = "jar",    Category = "Count",  SortOrder = 7 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000008"), Name = "Roll",         Symbol = "roll",   Category = "Count",  SortOrder = 8 },
        // Weight
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000009"), Name = "Gram",         Symbol = "g",      Category = "Weight", SortOrder = 10 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000010"), Name = "Kilogram",     Symbol = "kg",     Category = "Weight", SortOrder = 11 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000011"), Name = "Milligram",    Symbol = "mg",     Category = "Weight", SortOrder = 12 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000012"), Name = "Pound",        Symbol = "lb",     Category = "Weight", SortOrder = 13 },
        // Volume
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000014"), Name = "Milliliter",   Symbol = "mL",     Category = "Volume", SortOrder = 20 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000015"), Name = "Liter",        Symbol = "L",      Category = "Volume", SortOrder = 21 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000016"), Name = "Cup",          Symbol = "cup",    Category = "Volume", SortOrder = 22 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000017"), Name = "Tablespoon",   Symbol = "tbsp",   Category = "Volume", SortOrder = 23 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000018"), Name = "Teaspoon",     Symbol = "tsp",    Category = "Volume", SortOrder = 24 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000019"), Name = "Fluid Ounce",  Symbol = "fl oz",  Category = "Volume", SortOrder = 25 },
        // Length
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000020"), Name = "Meter",        Symbol = "m",      Category = "Length", SortOrder = 30 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000021"), Name = "Centimeter",   Symbol = "cm",     Category = "Length", SortOrder = 31 },
        new Unit { Id = new Guid("10000001-0000-0000-0000-000000000022"), Name = "Millimeter",   Symbol = "mm",     Category = "Length", SortOrder = 32 },
    ];
}
