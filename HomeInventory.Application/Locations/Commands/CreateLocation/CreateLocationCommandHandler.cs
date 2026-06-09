using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Results;
using HomeInventory.Application.Locations.Common;
using HomeInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Locations.Commands.CreateLocation;

public sealed class CreateLocationCommandHandler
    : IRequestHandler<CreateLocationCommand, Result<LocationDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _context;
    private readonly IQrSlugGenerator _slugGenerator;

    public CreateLocationCommandHandler(
        ICurrentUser currentUser,
        IApplicationDbContext context,
        IQrSlugGenerator slugGenerator)
    {
        _currentUser = currentUser;
        _context = context;
        _slugGenerator = slugGenerator;
    }

    public async Task<Result<LocationDto>> Handle(
        CreateLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HouseholdId is not { } householdId)
        {
            return Result.Failure<LocationDto>(HouseholdErrors.NoHousehold);
        }

        // Explicitly assert the parent belongs to the current household (defence in depth on top
        // of the global query filter), so a parent from another household is rejected.
        if (request.ParentId is { } parentId)
        {
            var parentExists = await _context.Locations
                .AnyAsync(l => l.Id == parentId && l.HouseholdId == householdId, cancellationToken);
            if (!parentExists)
            {
                return Result.Failure<LocationDto>(LocationErrors.ParentNotFound);
            }
        }

        var qrSlug = await GenerateUniqueSlugAsync(request.Name, householdId, cancellationToken);

        var location = new Location
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            ParentId = request.ParentId,
            Name = request.Name,
            Type = request.Type,
            QrSlug = qrSlug,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        return location.ToDto();
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string name,
        Guid householdId,
        CancellationToken cancellationToken)
    {
        string slug;
        do
        {
            slug = _slugGenerator.Generate(name);
        }
        while (await _context.Locations
            .AnyAsync(l => l.HouseholdId == householdId && l.QrSlug == slug, cancellationToken));

        return slug;
    }
}
