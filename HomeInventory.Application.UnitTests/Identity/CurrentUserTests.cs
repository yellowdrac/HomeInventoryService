using System.Security.Claims;
using FluentAssertions;
using HomeInventory.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace HomeInventory.Application.UnitTests.Identity;

public class CurrentUserTests
{
    private static CurrentUser BuildCurrentUser(HttpContext? httpContext)
    {
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return new CurrentUser(accessor);
    }

    private static HttpContext BuildHttpContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return context;
    }

    [Fact]
    public void Reads_user_id_and_household_id_from_claims()
    {
        var userId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var context = BuildHttpContext(
            new Claim(AppClaims.Subject, userId.ToString()),
            new Claim(AppClaims.HouseholdId, householdId.ToString()));

        var currentUser = BuildCurrentUser(context);

        currentUser.UserId.Should().Be(userId);
        currentUser.HouseholdId.Should().Be(householdId);
    }

    [Fact]
    public void Household_id_is_null_when_user_has_no_household_claim()
    {
        var userId = Guid.NewGuid();
        var context = BuildHttpContext(new Claim(AppClaims.Subject, userId.ToString()));

        var currentUser = BuildCurrentUser(context);

        currentUser.UserId.Should().Be(userId);
        currentUser.HouseholdId.Should().BeNull();
    }

    [Fact]
    public void Defaults_to_empty_when_request_is_unauthenticated()
    {
        var currentUser = BuildCurrentUser(httpContext: null);

        currentUser.UserId.Should().Be(Guid.Empty);
        currentUser.HouseholdId.Should().BeNull();
    }
}
