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
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

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

        return JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), Json)
            ?? throw new InvalidOperationException("Manifest is empty or invalid.");
    }

    public void Save(string projectRoot)
    {
        var path = Path.Combine(projectRoot, ".devstart.json");
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }
}
