using FluentValidation;

namespace HomeInventory.Application.Locations.Commands.MoveLocation;

public sealed class MoveLocationCommandValidator : AbstractValidator<MoveLocationCommand>
{
    public MoveLocationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
