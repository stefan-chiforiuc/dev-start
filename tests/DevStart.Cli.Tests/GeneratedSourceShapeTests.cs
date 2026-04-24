using System.Xml.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Scaffolds fresh projects into a sandbox and validates the shape of the
/// emitted files without invoking `dotnet build` — cheap enough to run on
/// every CI pass and catches token-subst bugs and malformed fragments.
///
/// This does NOT prove the project compiles against its NuGet refs; that's
/// the job of the real `dotnet build` step in CI. But it catches classes
/// of bugs (unreplaced tokens, unclosed braces, malformed XML) that
/// produce totally broken output.
/// </summary>
[Collection("SandboxCwd")]
public class GeneratedSourceShapeTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;

    public GeneratedSourceShapeTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-shape-").FullName;
        _priorCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_sandbox);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_priorCwd);
        try { Directory.Delete(_sandbox, recursive: true); } catch { /* best-effort */ }
        GC.SuppressFinalize(this);
    }

    public static TheoryData<string[], bool, string> Variations() => new()
    {
        // Baseline only.
        { [], false, "none" },
        // Default trio.
        { ["postgres", "auth", "otel"], false, "none" },
        // Everything the user can plausibly opt into in one go.
        { ["postgres", "auth", "otel", "cache", "queue", "s3", "mail", "flags", "sdk"], false, "fly" },
        // Multi-service with gateway + aca deploy.
        { ["postgres", "auth", "otel"], true, "aca" },
        // k8s capability (no source files, but still must scaffold cleanly).
        { ["postgres", "auth", "otel", "k8s"], false, "none" },
        // Frontend on the .NET stack.
        { ["postgres", "auth", "otel", "sdk", "frontend"], false, "none" },
    };

    [Theory]
    [MemberData(nameof(Variations))]
    public async Task Every_cs_parses_and_every_xml_is_well_formed(
        string[] caps, bool multiService, string deploy)
    {
        var name = $"shape-{caps.Length}-{deploy}-{(multiService ? "m" : "s")}";
        var planner = new Planner(name, multiService, caps, deploy, includeClaude: true);
        await planner.RunAsync();

        var root = Path.Combine(_sandbox, name);

        foreach (var cs in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(cs);
            var tree = CSharpSyntaxTree.ParseText(text);
            var errors = tree.GetDiagnostics()
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .ToList();
            errors.Should().BeEmpty(
                because: $"{cs} parsed with syntax errors:\n{string.Join("\n", errors.Select(e => e.ToString()))}");
        }

        foreach (var xml in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "*.props", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(root, "*.targets", SearchOption.AllDirectories)))
        {
            var act = () => XDocument.Load(xml);
            act.Should().NotThrow(because: $"{xml} must parse as XML");
        }

        foreach (var json in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .Where(p => !p.Contains("node_modules", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(json);
            var act = () => System.Text.Json.JsonDocument.Parse(text,
                new System.Text.Json.JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                });
            act.Should().NotThrow(because: $"{json} must parse as JSON");
        }

        // No unresolved tokens should leak through into the generated tree.
        foreach (var text in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(p => !p.Contains("/node_modules/", StringComparison.Ordinal))
            .Where(p => !p.EndsWith(".md", StringComparison.Ordinal)) // docs may reference {{Name}} in examples
            .Where(p => Path.GetFileName(p) != "CLAUDE.md")
            .Take(500))
        {
            var content = await File.ReadAllTextAsync(text);
            content.Should().NotContain("{{Name}}", because: $"{text} has an unresolved {{Name}} token");
            content.Should().NotContain("{{name}}", because: $"{text} has an unresolved {{name}} token");
        }
    }
}
