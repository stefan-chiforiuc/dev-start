using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using {{Name}}.Application.Storage;

namespace {{Name}}.Infrastructure.Storage;

internal sealed class S3ObjectStorage(IAmazonS3 s3, IOptions<StorageOptions> options) : IObjectStorage
{
    private readonly string _bucket = options.Value.Bucket;

    public async Task PutAsync(string key, Stream body, string? contentType = null, CancellationToken ct = default)
    {
        var req = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = body,
            ContentType = contentType ?? "application/octet-stream",
        };
        await s3.PutObjectAsync(req, ct);
    }

    public async Task<Stream> GetAsync(string key, CancellationToken ct = default)
    {
        var res = await s3.GetObjectAsync(_bucket, key, ct);
        return res.ResponseStream;
    }

    public Task<Uri> PresignGetAsync(string key, TimeSpan ttl, CancellationToken ct = default)
        => Presign(key, ttl, HttpVerb.GET, null);

    public Task<Uri> PresignPutAsync(string key, TimeSpan ttl, string? contentType = null, CancellationToken ct = default)
        => Presign(key, ttl, HttpVerb.PUT, contentType);

    private Task<Uri> Presign(string key, TimeSpan ttl, HttpVerb verb, string? contentType)
    {
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(ttl),
            Verb = verb,
            ContentType = contentType ?? "",
        };
        var url = s3.GetPreSignedURL(req);
        return Task.FromResult(new Uri(url));
    }
}
