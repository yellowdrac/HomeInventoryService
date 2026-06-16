using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Manages persisted refresh tokens (issuing and rotation). The implementation lives in
/// Infrastructure and stores tokens in the database.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generates and persists a new refresh token for a fresh login session.
    /// The session expiry is computed as <c>now + RefreshTokenDays</c>.
    /// </summary>
    Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Generates and persists a rotated refresh token that inherits the session expiry of the
    /// token being replaced. The expiry is NOT extended — use this for all refresh-token rotations.
    /// </summary>
    Task<IssuedRefreshToken> IssueRotatedAsync(Guid userId, DateTime sessionExpiresAtUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a refresh token and consumes it (rotation): on success the token is revoked
    /// and a <see cref="ConsumedRefreshToken"/> containing the user id and hard session-expiry
    /// is returned so the caller can issue a rotated pair without extending the session.
    /// </summary>
    Task<Result<ConsumedRefreshToken>> ValidateAndConsumeAsync(string refreshToken, CancellationToken cancellationToken);
}
