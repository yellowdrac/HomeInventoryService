using FluentValidation;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Behaviors;
using HomeInventory.Application.Common.Identity;
using HomeInventory.Application.Common.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HomeInventory.Application;

/// <summary>
/// Registration of the application-layer services in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddSingleton<IJoinCodeGenerator, JoinCodeGenerator>();
        services.AddSingleton<IQrSlugGenerator, QrSlugGenerator>();

        // Scoped: the stock service stages mutations on the per-request DbContext.
        services.AddScoped<IStockService, StockService>();

        return services;
    }
}
