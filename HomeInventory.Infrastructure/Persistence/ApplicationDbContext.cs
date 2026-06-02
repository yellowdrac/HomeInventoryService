using System.Reflection;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Domain.Common;
using HomeInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IApplicationDbContext"/>. Applies the
/// per-entity configurations, enables the Postgres extensions and wires up the
/// global multi-tenant filter by household.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUser _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Household> Households => Set<Household>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Item> Items => Set<Item>();

    public DbSet<StockLot> StockLots => Set<StockLot>();

    public DbSet<Movement> Movements => Set<Movement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Extensions for Spanish-language search; the initial migration will create them.
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplyHouseholdQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Applies a global <c>HouseholdId == currentUser.HouseholdId</c> filter to every
    /// entity that implements <see cref="IHouseholdScoped"/> (multi-tenancy).
    /// </summary>
    private void ApplyHouseholdQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IHouseholdScoped).IsAssignableFrom(entityType.ClrType))
            {
                typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetHouseholdFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, [modelBuilder]);
            }
        }
    }

    private void SetHouseholdFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IHouseholdScoped
    {
        // Access to _currentUser.HouseholdId is parameterized and evaluated per query.
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.HouseholdId == _currentUser.HouseholdId);
    }
}
