using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Common.Text;
using HomeInventory.Application.Items.Common;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Items.Commands.CreateItem;

public sealed class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, Result<ItemDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;

    public CreateItemCommandHandler(ICurrentUser currentUser, IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<ItemDto>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<ItemDto>(HouseholdErrors.NoHousehold);
        }

        var normalizedName = TextNormalization.Normalize(request.Name);

        var duplicate = await _context.Items
            .AnyAsync(
                i => i.HouseholdId == householdId && i.NormalizedName == normalizedName,
                cancellationToken);
        if (duplicate)
        {
            return Result.Failure<ItemDto>(ItemErrors.DuplicateName);
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Name = request.Name,
            NormalizedName = normalizedName,
            Category = request.Category,
            Barcode = request.Barcode,
            TrackingType = request.TrackingType,
            Unit = request.Unit,
            PhotoUrl = request.PhotoUrl,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return item.ToDto(totalQuantity: 0);
    }
}
