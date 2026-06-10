using FluentValidation;

namespace HomeInventory.Application.Items.Commands.DeleteItem;

public sealed class DeleteItemCommandValidator : AbstractValidator<DeleteItemCommand>
{
    public DeleteItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
