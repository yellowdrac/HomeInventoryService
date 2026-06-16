using FluentValidation;
using HomeInventory.Application.Assistant;
using HomeInventory.Application.Assistant.Tools;
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

        // Inventory assistant: the read-only tools (each wrapping a household-scoped MediatR query)
        // and the orchestrator. The LLM client, options and rate limiter are registered in
        // Infrastructure so the provider can be swapped without touching this layer.
        services.AddScoped<IAssistantTool, SearchInventoryTool>();
        services.AddScoped<IAssistantTool, GetItemDetailsTool>();
        services.AddScoped<IAssistantTool, GetLocationContentsTool>();
        services.AddScoped<IAssistantTool, ListLocationsTool>();
        services.AddScoped<IAssistantTool, GetExpiringStockTool>();
        services.AddScoped<IAssistantTool, GetInventorySummaryTool>();
        services.AddScoped<IInventoryAssistant, InventoryAssistant>();

        return services;
    }
}
