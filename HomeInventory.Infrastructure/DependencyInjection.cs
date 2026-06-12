using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Infrastructure.Identity;
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

        return services;
    }

    // Reads an S3 setting accepting both the ':' separator (Storage:S3:Name, produced by the
    // environment-variable provider from Storage__S3__Name) and the literal '__' separator stored
    // verbatim in user-secrets JSON (which does not translate '__' to ':').
    private static string ReadS3Setting(IConfiguration configuration, string name) =>
        configuration[$"Storage:S3:{name}"]
        ?? configuration[$"Storage__S3__{name}"]
        ?? string.Empty;
}
