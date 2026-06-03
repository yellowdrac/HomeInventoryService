using System.Security.Claims;
using HomeInventory.Application.Common.Abstractions;
using Microsoft.AspNetCore.Http;

namespace HomeInventory.Infrastructure.Identity;

/// <summary>
/// Resolves <see cref="ICurrentUser"/> from the JWT claims of the current HTTP request.
/// Returns <see cref="Guid.Empty"/> / <c>null</c> when the request is unauthenticated.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue(AppClaims.Subject), out var userId)
            ? userId
            : Guid.Empty;

    public Guid? HouseholdId =>
        Guid.TryParse(Principal?.FindFirstValue(AppClaims.HouseholdId), out var householdId)
            ? householdId
            : null;
}
