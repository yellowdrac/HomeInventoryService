using System.Text;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HomeInventory.Infrastructure.Identity;

/// <summary>Creates signed JWT access tokens using the symmetric key from <see cref="JwtOptions"/>.</summary>
public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException(
                "The JWT signing key is not configured. Set 'Jwt:SigningKey' in user-secrets or configuration.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(AuthUser user, DateTime sessionExpiresAtUtc)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            [AppClaims.Subject] = user.Id.ToString(),
            [AppClaims.Email] = user.Email,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [AppClaims.SessionExp] = new DateTimeOffset(sessionExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
        };

        if (user.HouseholdId is { } householdId)
        {
            claims[AppClaims.HouseholdId] = householdId.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAtUtc,
            SigningCredentials = _signingCredentials,
            Claims = claims,
        };

        var token = _tokenHandler.CreateToken(descriptor);
        return new AccessToken(token, expiresAtUtc);
    }
}
