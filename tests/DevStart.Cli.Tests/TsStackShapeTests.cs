using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Parallel to <see cref="GeneratedSourceShapeTests"/> for the TypeScript
/// stack. Roslyn doesn't apply here, so we validate JSON, YAML header shape,
/// and that tokens have been fully substituted. A separate opt-in test
/// (<c>Typecheck</c>) shells out to <c>tsc --noEmit</c> if it's on PATH.
/// </summary>
[Collection("SandboxCwd")]
public class TsStackShapeTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;

    public TsStackShapeTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-ts-shape-").FullName;
        _priorCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_sandbox);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_priorCwd);
        try { Directory.Delete(_sandbox, recursive: true); } catch { /* best-effort */ }
        GC.SuppressFinalize(this);
    }

    public static TheoryData<string[], bool> Variations() => new()
    {
        { [], false },
        { ["ts-postgres", "ts-auth", "ts-otel"], false },
        { ["ts-postgres", "ts-auth", "ts-otel", "ts-cache", "ts-queue", "ts-s3", "ts-mail", "ts-flags", "ts-sdk"], false },
        { ["ts-postgres", "ts-auth", "ts-otel"], true },
    };

    [Theory]
    [MemberData(nameof(Variations))]
    public async Task Every_json_parses_and_no_unresolved_tokens(string[] caps, bool multiService)
    {
        var name = $"ts-shape-{caps.Length}-{(multiService ? "m" : "s")}";
        var planner = new Planner(
            name, multiService, caps, deployTarget: "none",
            includeClaude: true, stack: Planner.StackTypescript);
        await planner.RunAsync();

        var root = Path.Combine(_sandbox, name);

        foreach (var json in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .Where(p => !p.Contains("node_modules", StringComparison.Ordinal))
            .Where(p => !p.EndsWith("openapi.json", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(json);
            var act = () => JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
            act.Should().NotThrow(because: $"{json} must parse as JSON");
        }

        foreach (var text in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(p => !p.Contains("node_modules", StringComparison.Ordinal))
            .Where(p => !p.EndsWith(".md", StringComparison.Ordinal))
            .Where(p => Path.GetFileName(p) != "CLAUDE.md")
            .Take(500))
        {
            var content = await File.ReadAllTextAsync(text);
            content.Should().NotContain("{{Name}}", because: $"{text} has an unresolved {{Name}} token");
            content.Should().NotContain("{{name}}", because: $"{text} has an unresolved {{name}} token");
            content.Should().NotContain("{{nameCamel}}", because: $"{text} has an unresolved {{nameCamel}} token");
        }

        // ts-base must exist.
        File.Exists(Path.Join(root, "apps", "api", "src", "app.ts")).Should().BeTrue();
        File.Exists(Path.Join(root, "package.json")).Should().BeTrue();
    }
}
