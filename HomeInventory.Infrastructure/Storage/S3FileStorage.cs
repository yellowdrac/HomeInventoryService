using Amazon.S3;
using Amazon.S3.Model;
using HomeInventory.Application.Common.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeInventory.Infrastructure.Storage;

/// <summary>
/// Amazon S3 implementation of <see cref="IFileStorage"/>. The bucket is private: objects are never
/// made public and reads happen exclusively through short-lived presigned GET URLs.
/// </summary>
public sealed class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly S3StorageOptions _options;

    public S3FileStorage(IAmazonS3 s3, IOptions<S3StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> SaveAsync(Stream content, string key, string contentType, CancellationToken ct)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };

        await _s3.PutObjectAsync(request, ct);
        return key;
    }

    public Task DeleteAsync(string key, CancellationToken ct) =>
        _s3.DeleteObjectAsync(
            new DeleteObjectRequest { BucketName = _options.BucketName, Key = key },
            ct);

    public string GetPresignedReadUrl(string key, TimeSpan ttl) =>
        _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
        });
}
