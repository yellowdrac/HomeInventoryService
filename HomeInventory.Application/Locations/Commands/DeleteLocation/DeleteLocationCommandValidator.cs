using FluentValidation;

namespace HomeInventory.Application.Locations.Commands.DeleteLocation;

public sealed class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
