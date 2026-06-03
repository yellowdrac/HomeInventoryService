using FluentValidation.TestHelper;
using HomeInventory.Application.Authentication.Commands.Login;
using Xunit;

namespace HomeInventory.Application.UnitTests.Authentication;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(new LoginCommand("user@example.com", "anything"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_email_fails()
    {
        var result = _validator.TestValidate(new LoginCommand("nope", "anything"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Empty_password_fails()
    {
        var result = _validator.TestValidate(new LoginCommand("user@example.com", ""));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
