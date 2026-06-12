namespace HomeInventory.Application.Items.Common;

/// <summary>
/// Constraints for item photos: the allowed image content types (mapped to their file
/// extension) and the maximum upload size.
/// </summary>
public static class ItemPhotoRules
{
    /// <summary>Maximum allowed upload size: 5 MB.</summary>
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    /// <summary>Allowed content types mapped to the extension used in the object key.</summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            ["image/webp"] = "webp",
        };

    /// <summary>
    /// Resolves the file extension for <paramref name="contentType"/>, or <see langword="null"/>
    /// when the content type is not an allowed image type.
    /// </summary>
    public static string? ResolveExtension(string? contentType) =>
        contentType is not null && AllowedContentTypes.TryGetValue(contentType, out var ext) ? ext : null;
}
