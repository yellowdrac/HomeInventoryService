using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using HomeInventory.Application.Assistant;
using HomeInventory.Application.Assistant.Llm;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Infrastructure.Assistant;
using HomeInventory.Infrastructure.Identity;
using HomeInventory.Infrastructure.Notifications;
using HomeInventory.Infrastructure.Persistence;
using HomeInventory.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeInventory.Infrastructure;

/// <summary>
/// Registration of the infrastructure services (persistence, identity, tokens) in the container.
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

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<ITokenService, TokenService>();

        // Amazon S3 file storage for item photos. Credentials come from Storage:S3:* (secrets/env).
        // Values are read accepting both the ':' separator (Storage:S3:*) and the '__' separator
        // (Storage__S3__*), so they bind whether they arrive as environment variables or as
        // user-secrets keyed with either separator.
        services.Configure<S3StorageOptions>(options =>
        {
            options.BucketName = ReadS3Setting(configuration, "BucketName");
            options.Region = ReadS3Setting(configuration, "Region");
            options.AccessKeyId = ReadS3Setting(configuration, "AccessKeyId");
            options.SecretAccessKey = ReadS3Setting(configuration, "SecretAccessKey");
        });

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;
            var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
            };
            return new AmazonS3Client(credentials, config);
        });
        services.AddSingleton<IFileStorage, S3FileStorage>();

        AddInventoryAssistant(services, configuration);
        AddNotifications(services, configuration);

        return services;
    }

    // Inventory assistant: cost knobs (Application options), per-user rate limiter and the concrete
    // LLM provider client. The provider, API key and model come from the 'Assistant' section
    // (env/user-secrets); only the LLM client is provider-specific, so swapping providers is a change
    // confined to this method.
    private static void AddInventoryAssistant(IServiceCollection services, IConfiguration configuration)
    {
        var assistantOptions = new AssistantOptions
        {
            MaxResponseTokens = configuration.GetValue("Assistant:MaxResponseTokens", 1024),
            MaxToolIterations = configuration.GetValue("Assistant:MaxToolIterations", 5),
            RateLimitPerMinute = configuration.GetValue("Assistant:RateLimitPerMinute", 10),
        };
        services.AddSingleton(assistantOptions);

        var provider = configuration["Assistant:Provider"] ?? "Anthropic";
        var isOpenAiCompatible = IsOpenAiCompatible(provider);

        var providerOptions = new AssistantProviderOptions
        {
            Provider = provider,
            // The API key may arrive as Assistant:ApiKey (env var Assistant__ApiKey, auto-translated)
            // or as a literal 'Assistant__ApiKey' key in user-secrets (which keeps the '__').
            ApiKey = configuration["Assistant:ApiKey"]
                ?? configuration["Assistant__ApiKey"]
                ?? string.Empty,
            Model = configuration["Assistant:Model"]
                ?? (isOpenAiCompatible ? "gemini-2.5-flash" : "claude-haiku-4-5"),
            // OpenAI-compatible providers vary, so the URL must be supplied; only the Anthropic
            // first-party endpoint has a sensible built-in default.
            BaseUrl = configuration["Assistant:BaseUrl"]
                ?? (isOpenAiCompatible ? string.Empty : "https://api.anthropic.com/v1/messages"),
            AnthropicVersion = configuration["Assistant:AnthropicVersion"] ?? "2023-06-01",
        };
        services.AddSingleton(providerOptions);

        services.AddSingleton<IAssistantRateLimiter, InMemoryAssistantRateLimiter>();

        // Select the concrete provider client. Adding another provider means another branch here only;
        // the Application layer (orchestrator, tools, command) is untouched.
        if (isOpenAiCompatible)
        {
            services.AddHttpClient<ILlmChatClient, OpenAiCompatibleChatClient>();
        }
        else
        {
            services.AddHttpClient<ILlmChatClient, AnthropicChatClient>();
        }
    }

    private static void AddNotifications(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));

        // Typed HttpClient for Resend email API.
        services.AddHttpClient<ResendEmailService>();
        services.AddScoped<IEmailService, ResendEmailService>();

        services.AddSingleton<IPushNotificationService, WebPushNotificationService>();

        services.AddHostedService<ExpirationNotificationWorker>();
    }

    // Anything that isn't first-party Anthropic is treated as an OpenAI-compatible /chat/completions
    // provider (Gemini's OpenAI endpoint, Groq, Cerebras, OpenRouter, Mistral, DeepSeek, Ollama, ...).
    private static bool IsOpenAiCompatible(string provider) =>
        !provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase);

    // Reads an S3 setting accepting both the ':' separator (Storage:S3:Name, produced by the
    // environment-variable provider from Storage__S3__Name) and the literal '__' separator stored
    // verbatim in user-secrets JSON (which does not translate '__' to ':').
    private static string ReadS3Setting(IConfiguration configuration, string name) =>
        configuration[$"Storage:S3:{name}"]
        ?? configuration[$"Storage__S3__{name}"]
        ?? string.Empty;
}
