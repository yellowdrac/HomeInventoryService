using FluentValidation;

namespace HomeInventory.Application.Stock.Commands.DiscardStock;

public sealed class DiscardStockCommandValidator : AbstractValidator<DiscardStockCommand>
{
    public DiscardStockCommandValidator()
    {
        RuleFor(x => x.StockLotId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
