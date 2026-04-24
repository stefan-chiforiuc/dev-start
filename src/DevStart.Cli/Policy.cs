using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevStart;

/// <summary>
/// Org-level policy bundle. Mirrors <see cref="Capability"/>'s shape: a
/// <c>policy.json</c> manifest + a <c>files/</c> tree + an injectors list
/// (reusing <see cref="InjectorSpec"/>). Loaded from embedded resources at
/// <c>policies/&lt;name&gt;/</c>.
/// </summary>
public sealed class Policy
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1.0";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("appliesToStacks")]
    public List<string> AppliesToStacks { get; set; } = [];

    [JsonPropertyName("extends")]
    public List<string> Extends { get; set; } = [];

    [JsonPropertyName("injectors")]
    public List<InjectorSpec> Injectors { get; set; } = [];

    [JsonPropertyName("validators")]
    public List<Validator> Validators { get; set; } = [];

    public sealed class Validator
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("check")] public string Check { get; set; } = "";
        [JsonPropertyName("path")] public string? Path { get; set; }
        [JsonPropertyName("pattern")] public string? Pattern { get; set; }
        [JsonPropertyName("allowlist")] public List<string>? Allowlist { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = "";
    }

    public static Policy LoadEmbedded(string name)
    {
        var json = ReadResource($"policies/{name}/policy.json")
            ?? throw new InvalidOperationException($"Unknown policy '{name}'.");
        return JsonSerializer.Deserialize<Policy>(json)
            ?? throw new InvalidOperationException($"Invalid policy.json for '{name}'.");
    }

    public static IEnumerable<string> AvailableNames()
    {
        return ResourceIndex.Keys
            .Where(n => n.StartsWith("policies/", StringComparison.Ordinal)
                     && n.EndsWith("/policy.json", StringComparison.Ordinal))
            .Select(n => n["policies/".Length..^"/policy.json".Length])
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal);
    }

    public static IEnumerable<string> FilesFor(string name)
    {
        var prefix = $"policies/{name}/files/";
        return ResourceIndex.Keys
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal))
            .Select(n => n[prefix.Length..]);
    }

    public static byte[]? ReadFile(string policyName, string relativePath)
        => ReadResourceBytes($"policies/{policyName}/files/{relativePath}");

    public static string? ReadFragment(string policyName, string fragmentPath)
        => ReadResource($"policies/{policyName}/fragments/{fragmentPath}");

    private static string? ReadResource(string logicalName)
    {
        var bytes = ReadResourceBytes(logicalName);
        return bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static byte[]? ReadResourceBytes(string logicalName)
    {
        var normalized = logicalName.Replace('\\', '/');
        if (!ResourceIndex.TryGetValue(normalized, out var actual)) return null;
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(actual);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static readonly Lazy<Dictionary<string, string>> _resourceIndex =
        new(BuildResourceIndex, isThreadSafe: true);
    private static Dictionary<string, string> ResourceIndex => _resourceIndex.Value;

    private static Dictionary<string, string> BuildResourceIndex()
    {
        var asm = Assembly.GetExecutingAssembly();
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in asm.GetManifestResourceNames())
        {
            map[name.Replace('\\', '/')] = name;
        }
        return map;
    }
}

/// <summary>Runs a policy's validator list against a project tree.</summary>
public static class PolicyValidatorRunner
{
    public sealed record Result(string PolicyName, string ValidatorId, bool Passed, string Message);

    public static IEnumerable<Result> Run(Policy policy, string projectRoot)
    {
        foreach (var v in policy.Validators)
        {
            var (passed, detail) = Evaluate(v, projectRoot);
            yield return new Result(policy.Name, v.Id, passed, passed ? "ok" : detail);
        }
    }

    private static (bool passed, string detail) Evaluate(Policy.Validator v, string root)
    {
        try
        {
            return v.Check switch
            {
                "file-exists" when v.Path is string p
                    => (File.Exists(Path.Join(root, p)), v.Message),
                "file-contains" when v.Path is string p && v.Pattern is string needle
                    => (File.Exists(Path.Join(root, p))
                        && System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(Path.Join(root, p)), needle),
                        v.Message),
                "image-allowlist" when v.Path is string p && v.Allowlist is { Count: > 0 } allowed
                    => CheckImageAllowlist(Path.Join(root, p), allowed, v.Message),
                _ => (false, $"unknown check '{v.Check}'"),
            };
        }
        catch (Exception ex)
        {
            return (false, $"error: {ex.Message}");
        }
    }

    private static (bool, string) CheckImageAllowlist(string dockerfile, IReadOnlyList<string> allowed, string message)
    {
        if (!File.Exists(dockerfile)) return (false, $"{dockerfile} missing");
        foreach (var raw in File.ReadAllLines(dockerfile))
        {
            var line = raw.TrimStart();
            if (!line.StartsWith("FROM ", StringComparison.OrdinalIgnoreCase)) continue;
            var image = line[5..].Split(new[] { ' ', '\t' }, 2)[0];
            if (!allowed.Any(prefix => image.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return (false, $"image '{image}' not in allowlist: {message}");
            }
        }
        return (true, message);
    }
}
