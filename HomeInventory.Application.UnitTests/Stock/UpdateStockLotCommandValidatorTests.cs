using FluentValidation.TestHelper;
using HomeInventory.Application.Stock.Commands.UpdateStockLot;
using Xunit;

namespace HomeInventory.Application.UnitTests.Stock;

public class UpdateStockLotCommandValidatorTests
{
    private readonly UpdateStockLotCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(new UpdateStockLotCommand(Guid.NewGuid(), 3, null, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_id_fails()
    {
        var result = _validator.TestValidate(new UpdateStockLotCommand(Guid.Empty, 3, null, null));

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Non_positive_quantity_fails()
    {
        var result = _validator.TestValidate(new UpdateStockLotCommand(Guid.NewGuid(), 0, null, null));

        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}
