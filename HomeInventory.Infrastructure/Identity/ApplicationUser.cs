using Microsoft.AspNetCore.Identity;

namespace HomeInventory.Infrastructure.Identity;

/// <summary>
/// Application user backed by ASP.NET Core Identity. <see cref="HouseholdId"/> stays null until
/// the user creates or joins a household.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public Guid? HouseholdId { get; set; }
}
