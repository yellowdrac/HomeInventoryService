using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Households.Commands.JoinHousehold;
using HomeInventory.Domain.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Households;

public class JoinHouseholdCommandHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();

    private JoinHouseholdCommandHandler BuildHandler(List<Household> households)
    {
        // Build the mock DbSet before any other Returns() call (see CreateHouseholdCommandHandlerTests).
        var householdsDbSet = households.BuildMockDbSet();
        _context.Households.Returns(householdsDbSet);
        _currentUser.UserId.Returns(_userId);
        _identityService.FindByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new AuthUser(_userId, "member@example.com", "Member", null));
        _identityService.SetHouseholdAsync(_userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _tokenService.CreateAccessToken(Arg.Any<AuthUser>())
            .Returns(new AccessToken("access", DateTime.UtcNow.AddMinutes(15)));
        _refreshTokenService.IssueAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new IssuedRefreshToken("refresh", DateTime.UtcNow.AddDays(7)));

        return new JoinHouseholdCommandHandler(
            _currentUser, _context, _identityService, _tokenService, _refreshTokenService);
    }

    [Fact]
    public async Task Assigns_household_when_the_join_code_is_valid()
    {
        _currentUser.HouseholdId.Returns((Guid?)null);
        var household = new Household { Id = Guid.NewGuid(), JoinCode = "ABCD2345", Name = "The Family" };
        var handler = BuildHandler([household]);

        // Lower-case input must be normalized before lookup.
        var result = await handler.Handle(new JoinHouseholdCommand("abcd2345"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _identityService.Received(1).SetHouseholdAsync(_userId, household.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_join_code_does_not_match_any_household()
    {
        _currentUser.HouseholdId.Returns((Guid?)null);
        var handler = BuildHandler([]);

        var result = await handler.Handle(new JoinHouseholdCommand("ZZZZ2345"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.InvalidJoinCode);
        await _identityService.DidNotReceive().SetHouseholdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_user_already_belongs_to_a_household()
    {
        _currentUser.HouseholdId.Returns(Guid.NewGuid());
        var handler = BuildHandler([]);

        var result = await handler.Handle(new JoinHouseholdCommand("ABCD2345"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.AlreadyInHousehold);
    }
}
