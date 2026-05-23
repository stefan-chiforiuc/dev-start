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
    public const int CurrentSchemaVersion = 3;

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

    /// <summary>
    /// Backend framework + version the project was scaffolded against. Lets
    /// `add`/`upgrade`/`doctor` rebuild the capability alias map without
    /// re-asking the user. Populated by the wizard or `--framework` flag.
    /// </summary>
    [JsonPropertyName("backend")]
    public BackendSelection? Backend { get; set; }

    public sealed class BackendSelection
    {
        [JsonPropertyName("framework")] public string Framework { get; set; } = "";
        [JsonPropertyName("version")] public string Version { get; set; } = "";
    }

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
    /// Bring an older manifest up to the current schema.
    /// v1 → v2: defaults <c>stack</c> and <c>policies</c>.
    /// v2 → v3: infers a <c>backend</c> selection from <c>stack</c> +
    /// installed capabilities. Existing projects ran against a single
    /// backend variant so the inference is unambiguous.
    /// </summary>
    private static Manifest Migrate(Manifest m)
    {
        if (m.SchemaVersion < 2)
        {
            if (string.IsNullOrEmpty(m.Stack)) m.Stack = "dotnet-api";
            m.Policies ??= [];
            m.SchemaVersion = 2;
        }
        if (m.SchemaVersion < 3)
        {
            m.Backend ??= InferBackend(m);
            m.SchemaVersion = 3;
        }
        return m;
    }

    private static BackendSelection InferBackend(Manifest m)
    {
        // Pre-v3 projects had exactly one backend variant per stack.
        if (m.Stack == "typescript-fastify")
        {
            return new BackendSelection { Framework = "fastify", Version = "5" };
        }
        return new BackendSelection { Framework = "aspnet", Version = "8" };
    }
}
