using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevStart;

/// <summary>
/// The <c>.devstart.json</c> manifest lives in every generated project.
/// It's the single source of truth for which capabilities are installed
/// and what version of the templates they came from.
/// </summary>
public sealed class Manifest
{
    public const int CurrentSchemaVersion = 2;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("stack")]
    public string Stack { get; set; } = "dotnet-api";

    [JsonPropertyName("templateVersion")]
    public string TemplateVersion { get; set; } = "0.1.0";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = [];

    [JsonPropertyName("services")]
    public List<string> Services { get; set; } = ["api"];

    [JsonPropertyName("deploy")]
    public string Deploy { get; set; } = "none";

    [JsonPropertyName("policies")]
    public List<string> Policies { get; set; } = [];

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Manifest Load(string projectRoot)
    {
        var path = Path.Combine(projectRoot, ".devstart.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"No .devstart.json in {projectRoot}. Run this from a dev-start-generated project.");
        }

        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), Json)
            ?? throw new InvalidOperationException("Manifest is empty or invalid.");

        return Migrate(manifest);
    }

    public void Save(string projectRoot)
    {
        var path = Path.Combine(projectRoot, ".devstart.json");
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>
    /// Bring an older manifest up to the current schema. v1 manifests predate
    /// the <c>stack</c> and <c>policies</c> fields; default them and bump.
    /// </summary>
    private static Manifest Migrate(Manifest m)
    {
        if (m.SchemaVersion < 2)
        {
            if (string.IsNullOrEmpty(m.Stack)) m.Stack = "dotnet-api";
            m.Policies ??= [];
            m.SchemaVersion = CurrentSchemaVersion;
        }
        return m;
    }
}
