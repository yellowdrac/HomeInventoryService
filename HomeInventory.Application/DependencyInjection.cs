using FluentValidation;
using HomeInventory.Application.Assistant;
using HomeInventory.Application.Assistant.Common;
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

        // Inventory assistant: read-only tools + write-proposal tools + the orchestrator.
        // The collector is Scoped so all proposal tools within one request share the same instance
        // and the orchestrator can read accumulated proposals after the tool loop completes.
        services.AddScoped<IProposedActionsCollector, ProposedActionsCollector>();
        services.AddScoped<IAssistantTool, SearchInventoryTool>();
        services.AddScoped<IAssistantTool, GetItemDetailsTool>();
        services.AddScoped<IAssistantTool, GetLocationContentsTool>();
        services.AddScoped<IAssistantTool, ListLocationsTool>();
        services.AddScoped<IAssistantTool, GetExpiringStockTool>();
        services.AddScoped<IAssistantTool, GetInventorySummaryTool>();
        services.AddScoped<IAssistantTool, ProposeCreateLocationTool>();
        services.AddScoped<IAssistantTool, ProposeCreateItemTool>();
        services.AddScoped<IAssistantTool, ProposeAddStockTool>();
        services.AddScoped<IAssistantTool, ProposeMoveStockTool>();
        services.AddScoped<IInventoryAssistant, InventoryAssistant>();

        return services;
    }
}
