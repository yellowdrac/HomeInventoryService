using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Units;

public sealed class GetUnitsQueryHandler : IRequestHandler<GetUnitsQuery, Result<IReadOnlyList<UnitDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetUnitsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<UnitDto>>> Handle(GetUnitsQuery request, CancellationToken cancellationToken)
    {
        var units = await _context.Units
            .OrderBy(u => u.SortOrder)
            .Select(u => new UnitDto(u.Id, u.Name, u.Symbol, u.Category, u.SortOrder))
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<UnitDto>>(units);
    }
}
