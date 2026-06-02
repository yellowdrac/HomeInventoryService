using Microsoft.Extensions.DependencyInjection;

namespace HomeInventory.Application;

/// <summary>
/// Registration of the application-layer services in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Phase 0: there are no MediatR handlers or FluentValidation validators yet.
        // The packages are referenced and ready; assembly-scanning registration
        // will be added in later phases, e.g.:
        //   services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        //   services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
