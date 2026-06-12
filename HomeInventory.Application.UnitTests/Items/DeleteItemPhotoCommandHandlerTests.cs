using FluentAssertions;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Items.Commands.DeleteItemPhoto;
using HomeInventory.Domain.Entities;
using HomeInventory.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class DeleteItemPhotoCommandHandlerTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();

    private DeleteItemPhotoCommandHandler BuildHandler(List<Item> items)
    {
        var itemsDbSet = items.BuildMockDbSet();
        _context.Items.Returns(itemsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.HouseholdId.Returns(_householdId);

        return new DeleteItemPhotoCommandHandler(_currentUser, _context, _fileStorage);
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

    [Fact]
    public async Task Deletes_the_object_and_clears_the_photo_url()
    {
        const string key = "households/h/items/i/photo.jpg";
        var item = NewItem(photoUrl: key);
        var handler = BuildHandler([item]);

        var result = await handler.Handle(new DeleteItemPhotoCommand(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync(key, Arg.Any<CancellationToken>());
        item.PhotoUrl.Should().BeNull();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Is_a_no_op_when_the_item_has_no_photo()
    {
        var handler = BuildHandler([NewItem()]);

        var result = await handler.Handle(new DeleteItemPhotoCommand(_itemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_an_item_from_another_household()
    {
        var handler = BuildHandler([NewItem(householdId: Guid.NewGuid(), photoUrl: "k")]);

        var result = await handler.Handle(new DeleteItemPhotoCommand(_itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ItemErrors.NotFound);
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_user_has_no_household()
    {
        var handler = BuildHandler([NewItem(photoUrl: "k")]);
        _currentUser.HouseholdId.Returns((Guid?)null);

        var result = await handler.Handle(new DeleteItemPhotoCommand(_itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HouseholdErrors.NoHousehold);
    }
}
