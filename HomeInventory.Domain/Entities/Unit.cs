namespace HomeInventory.Domain.Entities;

public sealed class Unit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;    // "Kilogram"
    public string Symbol { get; set; } = string.Empty;  // "kg"
    public string Category { get; set; } = string.Empty; // "Weight"
    public int SortOrder { get; set; }
}
