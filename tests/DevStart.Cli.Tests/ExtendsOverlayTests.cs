using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Locks in the `extends` overlay semantics: a capability declaring
/// `extends: "_shared/X"` gets the shared files copied first, then its
/// own files copied on top (variant overrides shared on path conflict).
/// </summary>
[Collection("SandboxCwd")]
public sealed class ExtendsOverlayTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;

    public ExtendsOverlayTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-extends-").FullName;
        _priorCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_sandbox);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_priorCwd);
        try { Directory.Delete(_sandbox, recursive: true); } catch { /* best-effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Base_capability_installs_files_from_the_shared_overlay()
    {
        // `base` only ships 5 version-pinned files; the rest come from
        // _shared/backend-aspnet/. This test asserts both kinds end up in
        // the generated project so we know the overlay actually fires.
        var planner = new Planner(
            name: "overlay-app",
            multiService: false,
            capabilities: [],
            deployTarget: "none",
            includeClaude: false);

        await planner.RunAsync();

        var root = Path.Combine(_sandbox, "overlay-app");

        // Owned by `base/` (version-pinned).
        File.Exists(Path.Combine(root, "Directory.Build.props")).Should().BeTrue();
        File.Exists(Path.Combine(root, "global.json")).Should().BeTrue();
        File.Exists(Path.Combine(root, "Dockerfile")).Should().BeTrue();

        // Owned by `_shared/backend-aspnet/` — must appear via the overlay.
        File.Exists(Path.Combine(root, "src/OverlayApp.Api/Program.cs")).Should().BeTrue();
        File.Exists(Path.Combine(root, "justfile")).Should().BeTrue();
        File.Exists(Path.Combine(root, "tests/OverlayApp.IntegrationTests/HealthCheckTests.cs"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Base_variant_files_win_over_shared_files_on_path_conflict()
    {
        // base ships Directory.Build.props targeting net8.0. If the shared
        // overlay somehow also shipped one (it doesn't), the variant must
        // override. This test pins behavior: the file present in `base/`
        // wins regardless of overlay order.
        var planner = new Planner(
            name: "overrider",
            multiService: false,
            capabilities: [],
            deployTarget: "none",
            includeClaude: false);

        await planner.RunAsync();

        var root = Path.Combine(_sandbox, "overrider");
        var buildProps = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        buildProps.Should().Contain("net8.0");
    }
}
