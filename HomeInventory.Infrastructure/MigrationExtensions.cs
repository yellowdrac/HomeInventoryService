using HomeInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeInventory.Infrastructure;

/// <summary>
/// Helpers to apply EF Core migrations at runtime.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Applies any pending migrations for the <see cref="ApplicationDbContext"/>. Intended for
    /// single-instance hosting (for example Render) when <c>RUN_MIGRATIONS_ON_STARTUP</c> is
    /// enabled. For multi-instance deployments, run migrations as a separate one-off step instead
    /// to avoid concurrent migration attempts.
    /// </summary>
    public static async Task ApplyMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
