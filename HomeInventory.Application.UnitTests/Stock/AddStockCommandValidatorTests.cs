using FluentValidation.TestHelper;
using HomeInventory.Application.Stock.Commands.AddStock;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class AddStockCommandValidatorTests
{
    private readonly AddStockCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(
            new AddStockCommand(Guid.NewGuid(), Guid.NewGuid(), 2, null, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_quantity_fails(decimal quantity)
    {
        var result = _validator.TestValidate(
            new AddStockCommand(Guid.NewGuid(), Guid.NewGuid(), quantity, null, null));

        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Empty_item_id_fails()
    {
        var result = _validator.TestValidate(
            new AddStockCommand(Guid.Empty, Guid.NewGuid(), 2, null, null));

        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }

    [Fact]
    public void Empty_location_id_fails()
    {
        var result = _validator.TestValidate(
            new AddStockCommand(Guid.NewGuid(), Guid.Empty, 2, null, null));

        result.ShouldHaveValidationErrorFor(x => x.LocationId);
    }
}
