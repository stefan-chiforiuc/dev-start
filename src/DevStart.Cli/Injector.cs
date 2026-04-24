using System.Text.Json.Serialization;

namespace DevStart;

public sealed class InjectorSpec
{
    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    /// <summary>
    /// Marker comment (e.g. <c>// devstart:api-services</c>). Fragment is
    /// inserted immediately after the marker line.
    /// </summary>
    [JsonPropertyName("marker")]
    public string? Marker { get; set; }

    /// <summary>
    /// Literal anchor string. Used with <see cref="Placement"/> to insert
    /// <em>before</em>, <em>after</em>, or <em>replace</em> the anchor.
    /// </summary>
    [JsonPropertyName("anchor")]
    public string? Anchor { get; set; }

    /// <summary>before | after | replace. Default is "after".</summary>
    [JsonPropertyName("placement")]
    public string Placement { get; set; } = "after";

    [JsonPropertyName("fragment")]
    public string Fragment { get; set; } = "";

    /// <summary>
    /// How the fragment is applied. <c>text</c> (default) uses marker/anchor
    /// string insertion; <c>json-merge</c> parses both sides as JSON and
    /// deep-merges the fragment object into the target (used for
    /// <c>package.json</c>, <c>tsconfig.json</c>, <c>.mcp.json</c>, etc.).
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "text";
}

public sealed class InjectorFile
{
    [JsonPropertyName("injectors")]
    public List<InjectorSpec> Injectors { get; set; } = [];
}
