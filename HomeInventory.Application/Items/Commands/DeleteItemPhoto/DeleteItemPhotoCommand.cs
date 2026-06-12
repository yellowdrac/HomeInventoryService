using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Items.Commands.DeleteItemPhoto;

/// <summary>
/// Removes the photo of an item in the current household: deletes the object from storage and
/// clears <c>Item.PhotoUrl</c>. Idempotent when the item has no photo.
/// </summary>
public sealed record DeleteItemPhotoCommand(Guid ItemId) : IRequest<Result>;
