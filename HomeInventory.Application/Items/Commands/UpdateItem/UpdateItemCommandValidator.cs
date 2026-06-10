using FluentValidation;

namespace HomeInventory.Application.Items.Commands.UpdateItem;

public sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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

        RuleFor(x => x.PhotoUrl)
            .MaximumLength(2048);
    }
}
