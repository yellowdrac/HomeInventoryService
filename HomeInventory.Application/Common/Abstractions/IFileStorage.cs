namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Abstraction over a private object store (implemented with Amazon S3 in Infrastructure).
/// Objects are addressed by an opaque <c>key</c>; reads happen through short-lived presigned
/// URLs so the bucket can stay private.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Uploads <paramref name="content"/> under <paramref name="key"/> with the given
    /// <paramref name="contentType"/> and returns the stored key.
    /// </summary>
    Task<string> SaveAsync(Stream content, string key, string contentType, CancellationToken ct);

    /// <summary>Deletes the object stored under <paramref name="key"/>.</summary>
    Task DeleteAsync(string key, CancellationToken ct);

    /// <summary>
    /// Returns a presigned GET URL for <paramref name="key"/> valid for <paramref name="ttl"/>.
    /// </summary>
    string GetPresignedReadUrl(string key, TimeSpan ttl);
}
