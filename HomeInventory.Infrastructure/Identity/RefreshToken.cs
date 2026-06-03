namespace HomeInventory.Infrastructure.Identity;

/// <summary>
/// Persisted refresh token. Tokens are single-use: once consumed they are revoked
/// (<see cref="RevokedAtUtc"/> set) and a new one is issued (rotation).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
