using FluentValidation;

namespace HomeInventory.Application.Items.Commands.UploadItemPhoto;

public sealed class UploadItemPhotoCommandValidator : AbstractValidator<UploadItemPhotoCommand>
{
    public UploadItemPhotoCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotNull();

        RuleFor(x => x.ContentType)
            .NotEmpty();

        RuleFor(x => x.Size)
            .GreaterThan(0);
    }
}
