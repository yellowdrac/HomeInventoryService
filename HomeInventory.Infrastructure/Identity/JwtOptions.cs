namespace HomeInventory.Infrastructure.Identity;

/// <summary>
/// JWT settings bound from the <c>Jwt</c> configuration section. The <see cref="SigningKey"/> is
/// expected to come from user-secrets (or another secret store), not from appsettings.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 7;
}
