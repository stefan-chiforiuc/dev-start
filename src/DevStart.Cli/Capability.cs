using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevStart;

public sealed class Capability
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1.0";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("dependsOn")]
    public List<string> DependsOn { get; set; } = [];

    [JsonPropertyName("conflictsWith")]
    public List<string> ConflictsWith { get; set; } = [];

    [JsonPropertyName("addsServices")]
    public List<string> AddsServices { get; set; } = [];

    [JsonPropertyName("envAdditions")]
    public List<EnvAddition> EnvAdditions { get; set; } = [];

    [JsonPropertyName("postInstall")]
    public List<string> PostInstall { get; set; } = [];

    [JsonPropertyName("doctor")]
    public List<DoctorCheck> Doctor { get; set; } = [];

    public sealed class EnvAddition
    {
        [JsonPropertyName("key")] public string Key { get; set; } = "";
        [JsonPropertyName("example")] public string Example { get; set; } = "";
    }

    public sealed class DoctorCheck
    {
        [JsonPropertyName("check")] public string Check { get; set; } = "";
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("port")] public int? Port { get; set; }
        [JsonPropertyName("path")] public string? Path { get; set; }
        [JsonPropertyName("min")] public string? Min { get; set; }
    }

    public static Capability LoadEmbedded(string name)
    {
        var json = ReadResource($"capabilities/{name}/capability.json")
            ?? throw new InvalidOperationException($"Unknown capability '{name}'.");
        return JsonSerializer.Deserialize<Capability>(json)
            ?? throw new InvalidOperationException($"Invalid capability.json for '{name}'.");
    }

    public static InjectorFile LoadInjectors(string name)
    {
        var json = ReadResource($"capabilities/{name}/injectors.json");
        return json is null
            ? new InjectorFile()
            : (JsonSerializer.Deserialize<InjectorFile>(json) ?? new InjectorFile());
    }

    public static IEnumerable<string> AvailableNames()
    {
        var asm = Assembly.GetExecutingAssembly();
        return asm.GetManifestResourceNames()
            .Where(n => n.StartsWith("capabilities/", StringComparison.Ordinal)
                     && n.EndsWith("/capability.json", StringComparison.Ordinal))
            .Select(n => n["capabilities/".Length..^"/capability.json".Length])
            .Where(n => !n.StartsWith('_'))
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal);
    }

    /// <summary>
    /// Enumerate files a capability wants to copy, keyed by path relative
    /// to the target project root (tokens unresolved).
    /// </summary>
    public static IEnumerable<string> FilesFor(string name)
    {
        var prefix = $"capabilities/{name}/files/";
        var asm = Assembly.GetExecutingAssembly();
        return asm.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal))
            .Select(n => n[prefix.Length..]);
    }

    public static byte[]? ReadFile(string capabilityName, string relativePath)
        => ReadResourceBytes($"capabilities/{capabilityName}/files/{relativePath}");

    public static string? ReadFragment(string capabilityName, string fragmentPath)
        => ReadResource($"capabilities/{capabilityName}/injectors/{fragmentPath}");

    private static string? ReadResource(string logicalName)
    {
        var bytes = ReadResourceBytes(logicalName);
        return bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static byte[]? ReadResourceBytes(string logicalName)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(logicalName);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
