using System.Security.Cryptography;
using HomeInventory.Application.Common.Abstractions;
using HomeInventory.Application.Common.Errors;
using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;
using HomeInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeInventory.Infrastructure.Identity;

/// <summary>
/// Persists and rotates refresh tokens in the database. Each token is single-use: validating it
/// also revokes it, so the caller must issue a new one.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtOptions _options;

    public RefreshTokenService(ApplicationDbContext context, IOptions<JwtOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessionExpiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
        return IssueRotatedAsync(userId, sessionExpiresAtUtc, cancellationToken);
    }

    public async Task<IssuedRefreshToken> IssueRotatedAsync(Guid userId, DateTime sessionExpiresAtUtc, CancellationToken cancellationToken)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = GenerateToken(),
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = sessionExpiresAtUtc,
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(refreshToken.Token, sessionExpiresAtUtc);
    }

    public async Task<Result<ConsumedRefreshToken>> ValidateAndConsumeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (stored is null || !stored.IsActive)
        {
            return Result.Failure<ConsumedRefreshToken>(AuthenticationErrors.InvalidRefreshToken);
        }

        stored.RevokedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new ConsumedRefreshToken(stored.UserId, stored.ExpiresAtUtc);
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
