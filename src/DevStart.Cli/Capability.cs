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

    /// <summary>
    /// Stack-specific dependency override. When a stack key matches the current
    /// project stack, its list is used instead of <see cref="DependsOn"/>. Lets
    /// a single capability (e.g. <c>frontend</c>) target <c>sdk</c> on the .NET
    /// stack and <c>ts-sdk</c> on the TypeScript stack without duplicating.
    /// </summary>
    [JsonPropertyName("dependsOnByStack")]
    public Dictionary<string, List<string>>? DependsOnByStack { get; set; }

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

    /// <summary>
    /// Declarative MCP servers contributed by this capability. Planner merges
    /// them all into <c>.mcp.json</c>; no per-capability if-blocks required.
    /// </summary>
    [JsonPropertyName("mcp")]
    public List<McpServerSpec> Mcp { get; set; } = [];

    /// <summary>
    /// Which stacks this capability targets. Empty = any. When set, <c>add</c>
    /// rejects installation on mismatched stacks.
    /// </summary>
    [JsonPropertyName("stacks")]
    public List<string> Stacks { get; set; } = [];

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
        [JsonPropertyName("args")] public string? Args { get; set; }
    }

    public sealed class McpServerSpec
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("command")] public string Command { get; set; } = "";
        [JsonPropertyName("args")] public List<string> Args { get; set; } = [];
        [JsonPropertyName("env")] public Dictionary<string, string>? Env { get; set; }
    }

    /// <summary>
    /// Dependencies applicable for a given stack — prefers
    /// <see cref="DependsOnByStack"/> when a matching key exists.
    /// </summary>
    public List<string> EffectiveDependsOn(string stack)
    {
        if (DependsOnByStack is { } map && map.TryGetValue(stack, out var scoped))
            return scoped;
        return DependsOn;
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
        return ResourceIndex.Keys
            .Where(n => n.StartsWith("capabilities/", StringComparison.Ordinal)
                     && n.EndsWith("/capability.json", StringComparison.Ordinal))
            .Select(n => n["capabilities/".Length..^"/capability.json".Length])
            .Where(n => !n.StartsWith('_'))
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal);
    }

    /// <summary>
    /// Enumerate files a capability wants to copy, keyed by path relative
    /// to the target project root (tokens unresolved). Paths use forward
    /// slashes on every platform.
    /// </summary>
    public static IEnumerable<string> FilesFor(string name)
    {
        var prefix = $"capabilities/{name}/files/";
        return ResourceIndex.Keys
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal))
            .Select(n => n[prefix.Length..]);
    }

    public static byte[]? ReadFile(string capabilityName, string relativePath)
        => ReadResourceBytes($"capabilities/{capabilityName}/files/{relativePath}");

    public static string? ReadFragment(string capabilityName, string fragmentPath)
        => ReadResource($"capabilities/{capabilityName}/injectors/{fragmentPath}");

    /// <summary>
    /// Resource names beginning with <paramref name="prefix"/> (e.g.
    /// <c>platform/compose/</c>), returned as forward-slash-normalized paths
    /// relative to the prefix. Paired with <see cref="ReadBytes"/> for lookup.
    /// </summary>
    public static IEnumerable<string> ResourceNamesUnder(string prefix)
    {
        return ResourceIndex.Keys
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal))
            .Select(n => n[prefix.Length..])
            .Where(n => n.Length > 0);
    }

    /// <summary>Raw bytes for a resource by its forward-slash-normalized name.</summary>
    public static byte[]? ReadBytes(string logicalName) => ReadResourceBytes(logicalName);

    private static string? ReadResource(string logicalName)
    {
        var bytes = ReadResourceBytes(logicalName);
        return bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static byte[]? ReadResourceBytes(string logicalName)
    {
        var normalized = Normalize(logicalName);
        if (!ResourceIndex.TryGetValue(normalized, out var actual)) return null;

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(actual);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// <para>
    /// Windows-built packs produce resource names with '\' (MSBuild's
    /// <c>%(RecursiveDir)</c> uses the platform separator); Linux builds use
    /// '/'. Both forms land as the assembly's manifest names depending on where
    /// the <c>.nupkg</c> was built.
    /// </para>
    /// <para>
    /// This index lets the rest of the CLI look up resources by a single
    /// canonical form (forward slashes) regardless of how the pack was built.
    /// </para>
    /// </summary>
    private static readonly Lazy<Dictionary<string, string>> _resourceIndex =
        new(BuildResourceIndex, isThreadSafe: true);

    private static Dictionary<string, string> ResourceIndex => _resourceIndex.Value;

    private static Dictionary<string, string> BuildResourceIndex()
    {
        var asm = Assembly.GetExecutingAssembly();
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in asm.GetManifestResourceNames())
        {
            map[Normalize(name)] = name;
        }
        return map;
    }

    private static string Normalize(string name) => name.Replace('\\', '/');
}
