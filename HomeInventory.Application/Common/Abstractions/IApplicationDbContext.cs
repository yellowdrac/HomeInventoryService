using HomeInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Persistence abstraction consumed by the application layer. Hides the concrete
/// <c>DbContext</c> (which lives in Infrastructure) to respect the dependency
/// direction of Clean Architecture.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Household> Households { get; }

    DbSet<Location> Locations { get; }

    DbSet<Item> Items { get; }

    DbSet<StockLot> StockLots { get; }

    DbSet<Movement> Movements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
