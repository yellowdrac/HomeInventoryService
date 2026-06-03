namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Generates the human-friendly codes used to invite members to a household.
/// </summary>
public interface IJoinCodeGenerator
{
    /// <summary>Produces a new random join code. Uniqueness is enforced by the caller/database.</summary>
    string Generate();
}
