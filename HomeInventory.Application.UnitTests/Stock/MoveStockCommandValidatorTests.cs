using FluentValidation.TestHelper;
using HomeInventory.Application.Stock.Commands.MoveStock;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class MoveStockCommandValidatorTests
{
    private readonly MoveStockCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(new MoveStockCommand(Guid.NewGuid(), Guid.NewGuid(), 2));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_quantity_fails(decimal quantity)
    {
        var result = _validator.TestValidate(new MoveStockCommand(Guid.NewGuid(), Guid.NewGuid(), quantity));

        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Empty_stock_lot_id_fails()
    {
        var result = _validator.TestValidate(new MoveStockCommand(Guid.Empty, Guid.NewGuid(), 2));

        result.ShouldHaveValidationErrorFor(x => x.StockLotId);
    }

    [Fact]
    public void Empty_destination_location_id_fails()
    {
        var result = _validator.TestValidate(new MoveStockCommand(Guid.NewGuid(), Guid.Empty, 2));

        result.ShouldHaveValidationErrorFor(x => x.ToLocationId);
    }
}
