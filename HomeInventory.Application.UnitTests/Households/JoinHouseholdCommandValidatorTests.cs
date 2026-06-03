using FluentValidation.TestHelper;
using HomeInventory.Application.Households.Commands.JoinHousehold;
using Xunit;

namespace HomeInventory.Application.UnitTests.Households;

public class JoinHouseholdCommandValidatorTests
{
    private readonly JoinHouseholdCommandValidator _validator = new();

    [Fact]
    public void Eight_character_code_passes()
    {
        var result = _validator.TestValidate(new JoinHouseholdCommand("ABCD2345"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("SHORT")]
    [InlineData("TOOLONGCODE")]
    public void Invalid_length_fails(string joinCode)
    {
        var result = _validator.TestValidate(new JoinHouseholdCommand(joinCode));

        result.ShouldHaveValidationErrorFor(x => x.JoinCode);
    }
}
