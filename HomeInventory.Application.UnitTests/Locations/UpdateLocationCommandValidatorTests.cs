using FluentValidation.TestHelper;
using HomeInventory.Application.Locations.Commands.UpdateLocation;
using HomeInventory.Domain.Enums;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class UpdateLocationCommandValidatorTests
{
    private readonly UpdateLocationCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(
            new UpdateLocationCommand(Guid.NewGuid(), "Pantry", LocationType.Furniture));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_id_fails()
    {
        var result = _validator.TestValidate(
            new UpdateLocationCommand(Guid.Empty, "Pantry", LocationType.Furniture));

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.TestValidate(
            new UpdateLocationCommand(Guid.NewGuid(), "", LocationType.Furniture));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
