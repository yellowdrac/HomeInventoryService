using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace HomeInventory.Infrastructure.Identity;

/// <summary>ASP.NET Core Identity-backed implementation of <see cref="IIdentityService"/>.</summary>
public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<AuthUser>> RegisterAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return Result.Failure<AuthUser>(AuthenticationErrors.EmailAlreadyInUse);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.Description));
            return Result.Failure<AuthUser>(AuthenticationErrors.RegistrationFailed(message));
        }

        return ToAuthUser(user);
    }

    public async Task<Result<AuthUser>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return Result.Failure<AuthUser>(AuthenticationErrors.InvalidCredentials);
        }

        return ToAuthUser(user);
    }

    public async Task<AuthUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToAuthUser(user);
    }

    public async Task<Result> SetHouseholdAsync(Guid userId, Guid householdId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(HouseholdErrors.UserNotFound);
        }

        user.HouseholdId = householdId;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Identity.UpdateFailed", message));
        }

        return Result.Success();
    }

    private static AuthUser ToAuthUser(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.HouseholdId);
}
