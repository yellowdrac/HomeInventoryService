namespace HomeInventory.Application.Common.Models;

/// <summary>A signed JWT access token together with its UTC expiry.</summary>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
