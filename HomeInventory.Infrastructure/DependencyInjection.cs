using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Infrastructure.Identity;
using HomeInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeInventory.Infrastructure;

/// <summary>
/// Registration of the infrastructure services (persistence, identity) in the container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "The 'Default' connection string was not found. Configure it in appsettings or user-secrets.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // TODO Phase 1: replace with an implementation based on the JWT claims.
        services.AddScoped<ICurrentUser, CurrentUserStub>();

        return services;
    }
}
