using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevStart;

/// <summary>
/// Per-file SHA-256 hashes captured when a file was last written by
/// <c>dev-start new</c>, <c>dev-start add</c>, or an applied upgrade.
/// Used by <c>dev-start upgrade --apply</c> to distinguish
/// user-edited files (disk != baseline) from template-refresh-only
/// files (disk == baseline).
///
/// Stored at <c>.devstart/baselines.json</c> — separate from the user-facing
/// <c>.devstart.json</c> so diffs of the manifest stay human-readable.
/// Committed to the repo so baselines travel with clones.
/// </summary>
public sealed class Baselines
{
    private const string RelativePath = ".devstart/baselines.json";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Map of path-relative-to-project → SHA-256 hex of the bytes we wrote.</summary>
    [JsonPropertyName("files")]
    public Dictionary<string, string> Files { get; set; } = new(StringComparer.Ordinal);

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Baselines Load(string projectRoot)
    {
        var path = Path.Join(projectRoot, RelativePath);
        if (!File.Exists(path)) return new Baselines();
        return JsonSerializer.Deserialize<Baselines>(File.ReadAllText(path), Json)
            ?? new Baselines();
    }

    public void Save(string projectRoot)
    {
        var path = Path.Join(projectRoot, RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>Hex-encoded SHA-256 of the bytes. Stable across platforms.</summary>
    public static string Hash(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    public static string Hash(string content) => Hash(System.Text.Encoding.UTF8.GetBytes(content));

    /// <summary>Record a file as written. Uses forward-slash path separators.</summary>
    public void Record(string relativePath, byte[] content)
    {
        Files[Normalize(relativePath)] = Hash(content);
    }

    public void Record(string relativePath, string content)
    {
        Files[Normalize(relativePath)] = Hash(content);
    }

    /// <summary>Returns the stored hash, or null if not tracked.</summary>
    public string? Get(string relativePath)
        => Files.TryGetValue(Normalize(relativePath), out var h) ? h : null;

    /// <summary>Drop a file from the baseline (e.g. capability removed).</summary>
    public void Forget(string relativePath) => Files.Remove(Normalize(relativePath));

    private static string Normalize(string path) => path.Replace('\\', '/');
}
