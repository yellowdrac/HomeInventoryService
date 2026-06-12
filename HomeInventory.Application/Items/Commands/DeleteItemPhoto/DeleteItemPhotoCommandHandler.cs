using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Items.Commands.DeleteItemPhoto;

public sealed class DeleteItemPhotoCommandHandler : IRequestHandler<DeleteItemPhotoCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;

    public DeleteItemPhotoCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IFileStorage fileStorage)
    {
        _currentUser = currentUser;
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<Result> Handle(DeleteItemPhotoCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure(HouseholdErrors.NoHousehold);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.Id == request.ItemId && i.HouseholdId == householdId,
                cancellationToken);
        if (item is null)
        {
            return Result.Failure(ItemErrors.NotFound);
        }

        // Nothing to do when the item has no photo.
        if (item.PhotoUrl is not { } key)
        {
            return Result.Success();
        }

        await _fileStorage.DeleteAsync(key, cancellationToken);

        item.PhotoUrl = null;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
