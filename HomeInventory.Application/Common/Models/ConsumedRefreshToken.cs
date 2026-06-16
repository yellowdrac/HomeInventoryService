namespace HomeInventory.Application.Common.Models;

/// <summary>
/// Result of consuming a refresh token. Carries the owning user id and the hard
/// session-expiry timestamp so the caller can issue a rotated token without extending
/// the original session window.
/// </summary>
public sealed record ConsumedRefreshToken(Guid UserId, DateTime SessionExpiresAtUtc);
