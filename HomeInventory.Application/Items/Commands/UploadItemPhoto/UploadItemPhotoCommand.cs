using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;

namespace HomeInventory.Application.Items.Commands.UploadItemPhoto;

/// <summary>
/// Uploads (or replaces) the photo of an item in the current household. The file is stored in the
/// private bucket and <c>Item.PhotoUrl</c> keeps the resulting object key; a fresh presigned GET
/// URL is returned.
/// </summary>
public sealed record UploadItemPhotoCommand(
    Guid ItemId,
    Stream Content,
    string ContentType,
    long Size)
    : IRequest<Result<ItemPhotoDto>>;
