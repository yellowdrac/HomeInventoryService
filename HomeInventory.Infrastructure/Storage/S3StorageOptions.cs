namespace HomeInventory.Infrastructure.Storage;

/// <summary>
/// S3 settings bound from the <c>Storage:S3</c> configuration section. Credentials are expected to
/// come from user-secrets or environment variables, never from appsettings.
/// </summary>
public sealed class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;
}
