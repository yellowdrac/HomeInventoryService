namespace HomeInventory.Application.Common.Abstractions;

/// <summary>
/// Generates short, stable, URL-friendly slugs that back the physical QR code of a location.
/// The application layer ensures the slug is unique within the household before persisting it.
/// </summary>
public interface IQrSlugGenerator
{
    /// <summary>Builds a slug derived from <paramref name="name"/> plus a random suffix.</summary>
    string Generate(string name);
}
