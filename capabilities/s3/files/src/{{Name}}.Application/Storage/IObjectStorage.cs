namespace {{Name}}.Application.Storage;

public interface IObjectStorage
{
    Task PutAsync(string key, Stream body, string? contentType = null, CancellationToken ct = default);
    Task<Stream> GetAsync(string key, CancellationToken ct = default);
    Task<Uri> PresignGetAsync(string key, TimeSpan ttl, CancellationToken ct = default);
    Task<Uri> PresignPutAsync(string key, TimeSpan ttl, string? contentType = null, CancellationToken ct = default);
}
