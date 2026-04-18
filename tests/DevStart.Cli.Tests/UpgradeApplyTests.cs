using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Scenario coverage for the 3-way-merge logic used by `dev-start upgrade --apply`.
/// We don't invoke the command; we exercise the pieces it's built from
/// (Planner.Render + Baselines) directly so each case is isolated.
/// </summary>
[Collection("SandboxCwd")]
public class UpgradeApplyTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;

    public UpgradeApplyTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-upgrade-").FullName;
        _priorCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_sandbox);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_priorCwd);
        try { Directory.Delete(_sandbox, recursive: true); }
        catch (IOException) { /* best-effort cleanup */ }
        catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Planner_records_baselines_for_every_generated_file()
    {
        var planner = new Planner("demo", multiService: false,
            capabilities: ["postgres"], deployTarget: "none", includeClaude: false);
        await planner.RunAsync();

        var root = Path.Join(_sandbox, "demo");
        var baselines = Baselines.Load(root);

        baselines.Files.Should().NotBeEmpty();
        baselines.Get("src/Demo.Api/Program.cs")
            .Should().NotBeNull("Program.cs was written by the base capability");
        baselines.Get("src/Demo.Infrastructure/Persistence/ApplicationDbContext.cs")
            .Should().NotBeNull("ApplicationDbContext.cs was written by the postgres capability");

        // Every recorded hash matches the actual file on disk.
        foreach (var (rel, expected) in baselines.Files)
        {
            var abs = Path.Join(root, rel);
            File.Exists(abs).Should().BeTrue($"baseline references {rel}");
            var actual = Baselines.Hash(File.ReadAllBytes(abs));
            actual.Should().Be(expected, $"{rel} hash must match its baseline");
        }
    }

    [Fact]
    public async Task Planner_stamps_manifest_with_cli_version()
    {
        var planner = new Planner("demo", multiService: false,
            capabilities: [], deployTarget: "none", includeClaude: false);
        await planner.RunAsync();

        var manifest = Manifest.Load(Path.Join(_sandbox, "demo"));
        manifest.TemplateVersion.Should().Be(CliVersion.Current,
            "the manifest records which CLI version scaffolded the project");
        manifest.TemplateVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CliVersion_is_stable_and_non_empty()
    {
        CliVersion.Current.Should().NotBeNullOrWhiteSpace();
        // Should not contain SourceLink's +<sha> suffix.
        CliVersion.Current.Should().NotContain("+");
    }

    [Fact]
    public void Hash_is_deterministic_across_invocations()
    {
        var a = Baselines.Hash("hello");
        var b = Baselines.Hash("hello");
        a.Should().Be(b);
        a.Should().NotBe(Baselines.Hash("hello "));
    }

    [Fact]
    public void Baselines_round_trip_through_disk()
    {
        var dir = Directory.CreateTempSubdirectory("devstart-bl-").FullName;
        try
        {
            var baselines = new Baselines();
            baselines.Record("src/a.cs", "class A {}");
            baselines.Record("src/b.cs", "class B {}");
            baselines.Save(dir);

            var loaded = Baselines.Load(dir);
            loaded.Get("src/a.cs").Should().Be(baselines.Get("src/a.cs"));
            loaded.Get("src/b.cs").Should().Be(baselines.Get("src/b.cs"));
            loaded.Get("src/c.cs").Should().BeNull();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Re_rendering_into_a_fresh_dir_produces_the_same_hashes()
    {
        // Invariant that upgrade depends on: Render is deterministic.
        // If this ever breaks, --apply will hallucinate conflicts on
        // every call because the "same" file produces different hashes.
        var planner = new Planner("demo", multiService: false,
            capabilities: ["postgres", "auth"], deployTarget: "none", includeClaude: true);
        await planner.RunAsync();

        var firstRoot = Path.Join(_sandbox, "demo");
        var firstBaselines = Baselines.Load(firstRoot);

        var second = Path.Join(_sandbox, "demo-second");
        var secondBaselines = planner.Render(second);

        foreach (var (rel, hash) in firstBaselines.Files)
        {
            secondBaselines.Get(rel).Should().Be(hash,
                $"{rel} must hash the same on a second render");
        }
    }
}
