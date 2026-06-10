using FluentValidation.TestHelper;
using HomeInventory.Application.Stock.Commands.DiscardStock;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class DiscardStockCommandValidatorTests
{
    private readonly DiscardStockCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(new DiscardStockCommand(Guid.NewGuid(), 2, "spoiled"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_quantity_fails(decimal quantity)
    {
        var result = _validator.TestValidate(new DiscardStockCommand(Guid.NewGuid(), quantity, null));

        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Empty_stock_lot_id_fails()
    {
        var result = _validator.TestValidate(new DiscardStockCommand(Guid.Empty, 2, null));

        result.ShouldHaveValidationErrorFor(x => x.StockLotId);
    }

    [Fact]
    public void Reason_longer_than_the_limit_fails()
    {
        var result = _validator.TestValidate(
            new DiscardStockCommand(Guid.NewGuid(), 2, new string('x', 501)));

        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}
