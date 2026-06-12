using FluentValidation.TestHelper;
using HomeInventory.Application.Items.Commands.UploadItemPhoto;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class UploadItemPhotoCommandValidatorTests
{
    private readonly UploadItemPhotoCommandValidator _validator = new();

    private static UploadItemPhotoCommand Command(
        Guid? itemId = null, Stream? content = null, string contentType = "image/jpeg", long size = 1024) =>
        new(itemId ?? Guid.NewGuid(), content ?? new MemoryStream([1]), contentType, size);

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(Command());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_item_id_fails()
    {
        var result = _validator.TestValidate(Command(itemId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }

    [Fact]
    public void Empty_content_type_fails()
    {
        var result = _validator.TestValidate(Command(contentType: ""));

        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_size_fails(long size)
    {
        var result = _validator.TestValidate(Command(size: size));

        result.ShouldHaveValidationErrorFor(x => x.Size);
    }
}
