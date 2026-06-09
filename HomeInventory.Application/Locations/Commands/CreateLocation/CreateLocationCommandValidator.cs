using FluentValidation;

namespace HomeInventory.Application.Locations.Commands.CreateLocation;

public sealed class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
