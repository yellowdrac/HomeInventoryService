using FluentValidation;

namespace HomeInventory.Application.Stock.Commands.ConsumeStock;

public sealed class ConsumeStockCommandValidator : AbstractValidator<ConsumeStockCommand>
{
    public ConsumeStockCommandValidator()
    {
        RuleFor(x => x.StockLotId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
