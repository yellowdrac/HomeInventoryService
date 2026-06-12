using FluentValidation.TestHelper;
using HomeInventory.Application.Items.Commands.CreateItem;
using HomeInventory.Domain.Enums;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class CreateItemCommandValidatorTests
{
    private readonly CreateItemCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(
            new CreateItemCommand("Batteries", "Electronics", "123456", TrackingType.Quantity, "unit"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.TestValidate(
            new CreateItemCommand("", null, null, TrackingType.Quantity, null));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Out_of_range_tracking_type_fails()
    {
        var result = _validator.TestValidate(
            new CreateItemCommand("Batteries", null, null, (TrackingType)42, null));

        result.ShouldHaveValidationErrorFor(x => x.TrackingType);
    }
}
