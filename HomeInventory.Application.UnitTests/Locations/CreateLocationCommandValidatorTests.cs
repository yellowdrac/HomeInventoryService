using FluentValidation.TestHelper;
using HomeInventory.Application.Locations.Commands.CreateLocation;
using HomeInventory.Domain.Enums;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class CreateLocationCommandValidatorTests
{
    private readonly CreateLocationCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(
            new CreateLocationCommand("Bedroom", LocationType.Room, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.TestValidate(
            new CreateLocationCommand("", LocationType.Room, null));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Too_long_name_fails()
    {
        var result = _validator.TestValidate(
            new CreateLocationCommand(new string('x', 201), LocationType.Room, null));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Out_of_range_type_fails()
    {
        var result = _validator.TestValidate(
            new CreateLocationCommand("Bedroom", (LocationType)999, null));

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }
}
