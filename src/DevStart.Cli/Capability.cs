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

    /// <summary>
    /// Load a capability's metadata from the embedded resources baked into
    /// the global tool. Capability files live under <c>capabilities/&lt;name&gt;/*</c>.
    /// </summary>
    public static Capability LoadEmbedded(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resource = $"capabilities/{name}/capability.json";
        using var stream = asm.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Unknown capability '{name}'.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<Capability>(json)
            ?? throw new InvalidOperationException($"Invalid capability.json for '{name}'.");
    }

    public static IEnumerable<string> AvailableNames()
    {
        var asm = Assembly.GetExecutingAssembly();
        return asm.GetManifestResourceNames()
            .Where(n => n.StartsWith("capabilities/", StringComparison.Ordinal) && n.EndsWith("/capability.json", StringComparison.Ordinal))
            .Select(n => n["capabilities/".Length..^"/capability.json".Length])
            .Where(n => !n.StartsWith('_'))
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal);
    }
}
