using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Items.Commands.UploadItemPhoto;

public sealed class UploadItemPhotoCommandHandler
    : IRequestHandler<UploadItemPhotoCommand, Result<ItemPhotoDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;

    public UploadItemPhotoCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IFileStorage fileStorage)
    {
        _currentUser = currentUser;
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<Result<ItemPhotoDto>> Handle(
        UploadItemPhotoCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<ItemPhotoDto>(HouseholdErrors.NoHousehold);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.Id == request.ItemId && i.HouseholdId == householdId,
                cancellationToken);
        if (item is null)
        {
            return Result.Failure<ItemPhotoDto>(ItemErrors.NotFound);
        }

        if (ItemPhotoRules.ResolveExtension(request.ContentType) is not { } extension)
        {
            return Result.Failure<ItemPhotoDto>(ItemErrors.PhotoContentTypeNotAllowed);
        }

        if (request.Size > ItemPhotoRules.MaxSizeBytes)
        {
            return Result.Failure<ItemPhotoDto>(ItemErrors.PhotoTooLarge);
        }

        var key = $"households/{householdId}/items/{item.Id}/{Guid.NewGuid()}.{extension}";

        // Replace the previous photo, if any, so orphan objects do not accumulate.
        if (item.PhotoUrl is { } previousKey)
        {
            await _fileStorage.DeleteAsync(previousKey, cancellationToken);
        }

        await _fileStorage.SaveAsync(request.Content, key, request.ContentType, cancellationToken);

        item.PhotoUrl = key;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var url = _fileStorage.GetPresignedReadUrl(key, FileStorageExtensions.DefaultReadUrlTtl);
        return new ItemPhotoDto(url);
    }
}
