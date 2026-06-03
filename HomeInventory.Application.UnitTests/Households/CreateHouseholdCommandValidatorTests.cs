using FluentValidation.TestHelper;
using HomeInventory.Application.Households.Commands.CreateHousehold;
using Xunit;

namespace HomeInventory.Application.UnitTests.Households;

public class CreateHouseholdCommandValidatorTests
{
    private readonly CreateHouseholdCommandValidator _validator = new();

    [Fact]
    public void Valid_name_passes()
    {
        var result = _validator.TestValidate(new CreateHouseholdCommand("The Doe Family"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.TestValidate(new CreateHouseholdCommand(""));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Too_long_name_fails()
    {
        var result = _validator.TestValidate(new CreateHouseholdCommand(new string('x', 201)));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
