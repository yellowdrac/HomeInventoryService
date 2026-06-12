using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Items.Commands.UploadItemPhoto;
using HomeInventory.Application.Items.Common;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class UploadItemPhotoCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();

    private UploadItemPhotoCommandHandler BuildHandler(List<Item> items)
    {
        var itemsDbSet = items.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);
        _fileStorage
            .SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<string>(1));
        _fileStorage.GetPresignedReadUrl(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns("https://signed.example/photo");

        return new UploadItemPhotoCommandHandler(_currentUser, _context, _fileStorage);
    }

    private Item NewItem(Guid? householdId = null, string? photoUrl = null) => new()
    {
        Id = _itemId,
        HouseholdId = householdId ?? _householdId,
        Name = "Batteries",
        NormalizedName = "batteries",
        TrackingType = TrackingType.Quantity,
        PhotoUrl = photoUrl,
    };

    private static UploadItemPhotoCommand Command(
        Guid itemId, string contentType = "image/jpeg", long size = 1024) =>
        new(itemId, new MemoryStream([1, 2, 3]), contentType, size);

    [Fact]
    public async Task Rejects_an_unsupported_content_type()
    {
        var handler = BuildHandler([NewItem()]);

        var result = await handler.Handle(
            Command(_itemId, contentType: "image/gif"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ItemErrors.PhotoContentTypeNotAllowed);
        await _fileStorage.DidNotReceive().SaveAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_file_over_the_maximum_size()
    {
        var handler = BuildHandler([NewItem()]);

        var result = await handler.Handle(
            Command(_itemId, size: ItemPhotoRules.MaxSizeBytes + 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ItemErrors.PhotoTooLarge);
        await _fileStorage.DidNotReceive().SaveAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stores_the_object_under_a_key_scoped_to_household_and_item()
    {
        var item = NewItem();
        var handler = BuildHandler([item]);

        var result = await handler.Handle(Command(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.PhotoUrl.Should().StartWith($"households/{_householdId}/items/{_itemId}/");
        item.PhotoUrl.Should().EndWith(".jpg");
        await _fileStorage.Received(1).SaveAsync(
            Arg.Any<Stream>(), item.PhotoUrl!, "image/jpeg", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replacing_an_existing_photo_deletes_the_old_object_and_updates_the_key()
    {
        const string previousKey = "households/old/items/old/old.png";
        var item = NewItem(photoUrl: previousKey);
        var handler = BuildHandler([item]);

        var result = await handler.Handle(Command(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync(previousKey, Arg.Any<CancellationToken>());
        item.PhotoUrl.Should().NotBe(previousKey);
        item.PhotoUrl.Should().StartWith($"households/{_householdId}/items/{_itemId}/");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_the_freshly_generated_presigned_url()
    {
        var handler = BuildHandler([NewItem()]);

        var result = await handler.Handle(Command(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PhotoUrl.Should().Be("https://signed.example/photo");
    }

    [Fact]
    public async Task Rejects_an_item_from_another_household()
    {
        var handler = BuildHandler([NewItem(householdId: Guid.NewGuid())]);

        var result = await handler.Handle(Command(_itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ItemErrors.NotFound);
        await _fileStorage.DidNotReceive().SaveAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        var handler = BuildHandler([NewItem()]);
        _currentUser.HouseholdId.Returns((Guid?)null);

        var result = await handler.Handle(Command(_itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }
}
