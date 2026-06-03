using FluentValidation.TestHelper;
using HomeInventory.Application.Authentication.Commands.RefreshToken;
using Xunit;

namespace HomeInventory.Application.UnitTests.Authentication;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Non_empty_token_passes()
    {
        var result = _validator.TestValidate(new RefreshTokenCommand("a-token"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_token_fails()
    {
        var result = _validator.TestValidate(new RefreshTokenCommand(""));

        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
