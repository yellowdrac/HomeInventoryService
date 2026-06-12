namespace HomeInventory.Application.Common.Abstractions;

/// <summary>Convenience helpers for <see cref="IFileStorage"/>.</summary>
public static class FileStorageExtensions
{
    /// <summary>Default lifetime of the presigned read URLs surfaced in read models (1 hour).</summary>
    public static readonly TimeSpan DefaultReadUrlTtl = TimeSpan.FromHours(1);

    /// <summary>
    /// Returns a fresh presigned GET URL for <paramref name="key"/> (using
    /// <see cref="DefaultReadUrlTtl"/>), or <see langword="null"/> when there is no key.
    /// </summary>
    public static string? GetPresignedReadUrlOrNull(this IFileStorage storage, string? key) =>
        string.IsNullOrEmpty(key) ? null : storage.GetPresignedReadUrl(key, DefaultReadUrlTtl);
}
