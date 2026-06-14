using FluentValidation.TestHelper;
using HomeInventory.Application.Assistant.Commands.AskAssistant;
using Xunit;

namespace HomeInventory.Application.UnitTests.Assistant;

public class AskAssistantCommandValidatorTests
{
    private readonly AskAssistantCommandValidator _validator = new();

    [Fact]
    public void Valid_message_passes()
    {
        var result = _validator.TestValidate(new AskAssistantCommand("where are my batteries?"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_message_fails(string message)
    {
        var result = _validator.TestValidate(new AskAssistantCommand(message));

        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void Overlong_message_fails()
    {
        var message = new string('a', AskAssistantCommandValidator.MaxMessageLength + 1);

        var result = _validator.TestValidate(new AskAssistantCommand(message));

        result.ShouldHaveValidationErrorFor(x => x.Message);
    }
}
