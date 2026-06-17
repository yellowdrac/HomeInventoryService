using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Items.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Items.Queries.GetItemById;

public sealed class GetItemByIdQueryHandler : IRequestHandler<GetItemByIdQuery, Result<ItemDetailDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;

    public GetItemByIdQueryHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IFileStorage fileStorage)
    {
        _currentUser = currentUser;
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<Result<ItemDetailDto>> Handle(
        GetItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<ItemDetailDto>(HouseholdErrors.NoHousehold);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.Id == request.Id && i.HouseholdId == householdId,
                cancellationToken);
        if (item is null)
        {
            return Result.Failure<ItemDetailDto>(ItemErrors.NotFound);
        }

        var lots = await _context.StockLots
            .Where(s => s.ItemId == item.Id && s.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var locationsById = await _context.Locations
            .Where(l => l.HouseholdId == householdId)
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var itemsById = new Dictionary<Guid, Domain.Entities.Item> { [item.Id] = item };

        // FEFO: surface the lots that expire first, with the non-perishable ones (no date) last.
        var lotDtos = lots
            .OrderBy(l => l.ExpirationDate.HasValue ? 0 : 1)
            .ThenBy(l => l.ExpirationDate)
            .Select(lot => StockLotDtoFactory.Build(lot, itemsById, locationsById))
            .ToList();

        var totalQuantity = lots.Sum(l => l.Quantity);

        return new ItemDetailDto(
            item.Id,
            item.Name,
            item.Category,
            item.Barcode,
            item.TrackingType,
            item.Unit,
            _fileStorage.GetPresignedReadUrlOrNull(item.PhotoUrl),
            totalQuantity,
            item.MinimumQuantity,
            lotDtos);
    }
}
