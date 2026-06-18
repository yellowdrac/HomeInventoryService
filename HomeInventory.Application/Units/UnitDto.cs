namespace HomeInventory.Application.Units;

public sealed record UnitDto(Guid Id, string Name, string Symbol, string Category, int SortOrder);
