using FluentValidation;

namespace HomeInventory.Application.Items.Commands.CreateItem;

public sealed class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TrackingType)
            .IsInEnum();

        RuleFor(x => x.Category)
            .MaximumLength(100);

        RuleFor(x => x.Barcode)
            .MaximumLength(64);

        RuleFor(x => x.Unit)
            .MaximumLength(32);
    }
}
