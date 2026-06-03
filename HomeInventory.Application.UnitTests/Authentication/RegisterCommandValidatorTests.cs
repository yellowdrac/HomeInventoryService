using FluentValidation.TestHelper;
using HomeInventory.Application.Authentication.Commands.Register;
using Xunit;

namespace HomeInventory.Application.UnitTests.Authentication;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var command = new RegisterCommand("user@example.com", "Sup3rSecret", "Jane Doe");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_email_fails(string email)
    {
        var command = new RegisterCommand(email, "Sup3rSecret", "Jane Doe");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Invalid_password_fails(string password)
    {
        var command = new RegisterCommand("user@example.com", password, "Jane Doe");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Empty_display_name_fails()
    {
        var command = new RegisterCommand("user@example.com", "Sup3rSecret", "");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }
}
