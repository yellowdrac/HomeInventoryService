using FluentValidation;

namespace HomeInventory.Application.Items.Commands.DeleteItemPhoto;

public sealed class DeleteItemPhotoCommandValidator : AbstractValidator<DeleteItemPhotoCommand>
{
    public DeleteItemPhotoCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty();
    }
}
