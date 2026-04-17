using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Exercises the `dev-start add` pipeline against an already-scaffolded project.
/// Uses <see cref="CapabilityInstaller"/> directly to avoid spinning up the CLI.
/// </summary>
[Collection("SandboxCwd")]
public class AddCapabilityTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;
    private readonly string _projectRoot;
    private readonly Tokens _tokens;

    public AddCapabilityTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-add-").FullName;
        _priorCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_sandbox);

        // Scaffold a baseline project (postgres + auth + otel).
        new Planner("my-app", false, ["postgres", "auth", "otel"], "none", includeClaude: false)
            .RunAsync().GetAwaiter().GetResult();

        _projectRoot = Path.Combine(_sandbox, "my-app");
        _tokens = new Tokens("my-app");
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_priorCwd);
        try { Directory.Delete(_sandbox, recursive: true); } catch { /* best-effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Add_cache_into_existing_project_wires_everything()
    {
        CapabilityInstaller.Install("cache", _projectRoot, _tokens);

        File.Exists(Path.Combine(_projectRoot, "src/MyApp.Infrastructure/Caching/ITypedCache.cs"))
            .Should().BeTrue();
        File.Exists(Path.Combine(_projectRoot, "src/MyApp.Infrastructure/CacheModule.cs"))
            .Should().BeTrue();

        var infraDi = File.ReadAllText(
            Path.Combine(_projectRoot, "src/MyApp.Infrastructure/DependencyInjection.cs"));
        infraDi.Should().Contain("services.AddCache(config);");
        infraDi.Should().Contain("services.AddPostgres(config);"); // earlier injection preserved

        var appsettings = File.ReadAllText(
            Path.Combine(_projectRoot, "src/MyApp.Api/appsettings.json"));
        appsettings.Should().Contain("\"Redis\"");
        appsettings.Should().Contain("ConnectionStrings"); // earlier injection preserved
    }

    [Fact]
    public void Installing_twice_is_idempotent()
    {
        CapabilityInstaller.Install("cache", _projectRoot, _tokens);
        var once = File.ReadAllText(
            Path.Combine(_projectRoot, "src/MyApp.Infrastructure/DependencyInjection.cs"));

        CapabilityInstaller.Install("cache", _projectRoot, _tokens);
        var twice = File.ReadAllText(
            Path.Combine(_projectRoot, "src/MyApp.Infrastructure/DependencyInjection.cs"));

        var occurrences = System.Text.RegularExpressions.Regex
            .Matches(twice, @"services\.AddCache\(config\);")
            .Count;
        occurrences.Should().Be(1, because: "the installer must skip already-applied fragments");
    }
}
