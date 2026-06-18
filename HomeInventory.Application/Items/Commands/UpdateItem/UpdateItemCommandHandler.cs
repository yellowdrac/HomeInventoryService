using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Common.Text;
using HomeInventory.Application.Items.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Items.Commands.UpdateItem;

public sealed class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand, Result<ItemDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;

    public UpdateItemCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IFileStorage fileStorage)
    {
        _currentUser = currentUser;
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<Result<ItemDto>> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<ItemDto>(HouseholdErrors.NoHousehold);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.Id == request.Id && i.HouseholdId == householdId,
                cancellationToken);
        if (item is null)
        {
            return Result.Failure<ItemDto>(ItemErrors.NotFound);
        }

        var normalizedName = TextNormalization.Normalize(request.Name);
        if (normalizedName != item.NormalizedName)
        {
            var duplicate = await _context.Items
                .AnyAsync(
                    i => i.HouseholdId == householdId
                        && i.NormalizedName == normalizedName
                        && i.Id != item.Id,
                    cancellationToken);
            if (duplicate)
            {
                return Result.Failure<ItemDto>(ItemErrors.DuplicateName);
            }

            item.NormalizedName = normalizedName;
        }

        item.Name = request.Name;
        item.Category = request.Category;
        item.Barcode = request.Barcode;
        item.TrackingType = request.TrackingType;
        item.UnitId = request.UnitId;
        item.MinimumQuantity = request.MinimumQuantity;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var totalQuantity = await _context.StockLots
            .Where(s => s.ItemId == item.Id && s.HouseholdId == householdId)
            .SumAsync(s => s.Quantity, cancellationToken);

        var unitSymbol = request.UnitId.HasValue
            ? (await _context.Units.FindAsync([request.UnitId.Value], cancellationToken))?.Symbol
            : null;

        return item.ToDto(totalQuantity, _fileStorage.GetPresignedReadUrlOrNull(item.PhotoUrl), unitSymbol);
    }
}
