namespace HomeInventory.Application.Common.Models;

/// <summary>A freshly issued refresh token together with its UTC expiry.</summary>
public sealed record IssuedRefreshToken(string Value, DateTime ExpiresAtUtc);
