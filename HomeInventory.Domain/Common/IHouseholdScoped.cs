namespace HomeInventory.Domain.Common;

/// <summary>
/// Marks an entity as belonging to a household (tenant). Enables the global
/// multi-tenant filtering by <see cref="HouseholdId"/>.
/// </summary>
public interface IHouseholdScoped
{
    Guid HouseholdId { get; set; }
}
