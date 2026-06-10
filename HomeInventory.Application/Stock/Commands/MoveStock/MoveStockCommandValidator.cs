using FluentValidation;

namespace HomeInventory.Application.Stock.Commands.MoveStock;

public sealed class MoveStockCommandValidator : AbstractValidator<MoveStockCommand>
{
    public MoveStockCommandValidator()
    {
        RuleFor(x => x.StockLotId)
            .NotEmpty();

        RuleFor(x => x.ToLocationId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}
