using FluentValidation.TestHelper;
using HomeInventory.Application.Items.Commands.DeleteItemPhoto;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class DeleteItemPhotoCommandValidatorTests
{
    private readonly DeleteItemPhotoCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(new DeleteItemPhotoCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_item_id_fails()
    {
        var result = _validator.TestValidate(new DeleteItemPhotoCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }
}
