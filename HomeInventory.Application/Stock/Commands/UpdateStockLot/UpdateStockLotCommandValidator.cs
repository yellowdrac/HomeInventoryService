using FluentValidation;

namespace HomeInventory.Application.Stock.Commands.UpdateStockLot;

public sealed class UpdateStockLotCommandValidator : AbstractValidator<UpdateStockLotCommand>
{
    public UpdateStockLotCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}
