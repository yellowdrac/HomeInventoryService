using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Households.Commands.CreateHousehold;
using HomeInventory.Domain.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Households;

public class CreateHouseholdCommandHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IJoinCodeGenerator _joinCodeGenerator = Substitute.For<IJoinCodeGenerator>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();

    private CreateHouseholdCommandHandler BuildHandler(List<Household> households)
    {
        // Build the mock DbSet before any other Returns() call: BuildMockDbSet configures its own
        // NSubstitute substitutes, which corrupts call tracking if nested inside another Returns().
        var householdsDbSet = households.BuildMockDbSet();
        _context.Households.Returns(householdsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _currentUser.UserId.Returns(_userId);
        _identityService.FindByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new AuthUser(_userId, "owner@example.com", "Owner", null));
        _identityService.SetHouseholdAsync(_userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _tokenService.CreateAccessToken(Arg.Any<AuthUser>())
            .Returns(new AccessToken("access", DateTime.UtcNow.AddMinutes(15)));
        _refreshTokenService.IssueAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new IssuedRefreshToken("refresh", DateTime.UtcNow.AddDays(7)));

        return new CreateHouseholdCommandHandler(
            _currentUser, _context, _identityService, _joinCodeGenerator, _tokenService, _refreshTokenService);
    }

    [Fact]
    public async Task Creates_household_with_current_user_as_owner_and_assigns_it()
    {
        _currentUser.HouseholdId.Returns((Guid?)null);
        _joinCodeGenerator.Generate().Returns("ABCD2345");
        var handler = BuildHandler([]);

        var result = await handler.Handle(new CreateHouseholdCommand("My Home"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access");
        result.Value.RefreshToken.Should().Be("refresh");

        _context.Households.Received(1).Add(Arg.Is<Household>(h =>
            h.OwnerUserId == _userId && h.Name == "My Home" && h.JoinCode == "ABCD2345"));
        await _identityService.Received(1).SetHouseholdAsync(_userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        // The re-issued access token must carry the new household id.
        _tokenService.Received().CreateAccessToken(Arg.Is<AuthUser>(u => u.HouseholdId != null));
    }

    [Fact]
    public async Task Retries_until_the_join_code_is_unique()
    {
        _currentUser.HouseholdId.Returns((Guid?)null);
        var existing = new Household { Id = Guid.NewGuid(), JoinCode = "DUP12345", Name = "Other" };
        _joinCodeGenerator.Generate().Returns("DUP12345", "NEW12345");
        var handler = BuildHandler([existing]);

        var result = await handler.Handle(new CreateHouseholdCommand("My Home"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _joinCodeGenerator.Received(2).Generate();
        _context.Households.Received(1).Add(Arg.Is<Household>(h => h.JoinCode == "NEW12345"));
    }

    [Fact]
    public async Task Fails_when_user_already_belongs_to_a_household()
    {
        _currentUser.HouseholdId.Returns(Guid.NewGuid());
        var handler = BuildHandler([]);

        var result = await handler.Handle(new CreateHouseholdCommand("My Home"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.AlreadyInHousehold);
        _context.Households.DidNotReceive().Add(Arg.Any<Household>());
    }
}
