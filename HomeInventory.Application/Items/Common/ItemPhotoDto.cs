namespace HomeInventory.Application.Items.Common;

/// <summary>Result of uploading an item photo: a fresh presigned GET URL for the new object.</summary>
public sealed record ItemPhotoDto(string PhotoUrl);
