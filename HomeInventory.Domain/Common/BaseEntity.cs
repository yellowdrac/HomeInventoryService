namespace HomeInventory.Domain.Common;

/// <summary>
/// Base for all persisted entities. Provides identity and audit timestamps.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
