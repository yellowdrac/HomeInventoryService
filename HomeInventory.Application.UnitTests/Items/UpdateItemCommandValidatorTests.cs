using FluentValidation.TestHelper;
using HomeInventory.Application.Items.Commands.UpdateItem;
using HomeInventory.Domain.Enums;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class UpdateItemCommandValidatorTests
{
    private readonly UpdateItemCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(
            new UpdateItemCommand(Guid.NewGuid(), "Batteries", null, null, TrackingType.Quantity, "unit"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_id_fails()
    {
        var result = _validator.TestValidate(
            new UpdateItemCommand(Guid.Empty, "Batteries", null, null, TrackingType.Quantity, null));

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.TestValidate(
            new UpdateItemCommand(Guid.NewGuid(), "", null, null, TrackingType.Quantity, null));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
