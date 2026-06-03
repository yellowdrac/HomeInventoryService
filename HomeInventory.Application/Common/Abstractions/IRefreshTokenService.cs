using HomeInventory.Application.Common.Models;
using HomeInventory.Application.Common.Results;

namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Manages persisted refresh tokens (issuing and rotation). The implementation lives in
/// Infrastructure and stores tokens in the database.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Generates and persists a new refresh token for the user.</summary>
    Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a refresh token and consumes it (rotation): on success the token is revoked
    /// and the owning user id is returned so the caller can issue a fresh pair.
    /// </summary>
    Task<Result<Guid>> ValidateAndConsumeAsync(string refreshToken, CancellationToken cancellationToken);
}
