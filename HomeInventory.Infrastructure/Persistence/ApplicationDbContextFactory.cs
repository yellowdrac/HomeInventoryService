using HomeInventory.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeInventory.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so that the <c>dotnet ef</c> tools can instantiate the
/// context without starting the API (pointing at Infrastructure as the startup
/// project).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Allows overriding the connection string via environment variable; otherwise uses local Postgres.
        var connectionString =
            Environment.GetEnvironmentVariable("HOMEINVENTORY_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=homeinventory;Username=postgres;Password=postgres123";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options, new CurrentUserStub());
    }
}
