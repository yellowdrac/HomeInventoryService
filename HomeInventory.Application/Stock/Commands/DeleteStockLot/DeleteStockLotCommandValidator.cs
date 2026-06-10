using FluentValidation;

namespace HomeInventory.Application.Stock.Commands.DeleteStockLot;

public sealed class DeleteStockLotCommandValidator : AbstractValidator<DeleteStockLotCommand>
{
    public DeleteStockLotCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
