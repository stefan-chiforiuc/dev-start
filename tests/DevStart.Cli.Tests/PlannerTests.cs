using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

[Collection("SandboxCwd")]
public class PlannerTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;

    public PlannerTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-planner-").FullName;
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
    public async Task Generates_a_project_skeleton_with_the_default_capabilities()
    {
        var planner = new Planner(
            name: "my-app",
            multiService: false,
            capabilities: ["postgres", "auth", "otel"],
            deployTarget: "none",
            includeClaude: true);

        await planner.RunAsync();

        var root = Path.Combine(_sandbox, "my-app");
        Directory.Exists(root).Should().BeTrue();

        // Manifest written.
        File.Exists(Path.Combine(root, ".devstart.json")).Should().BeTrue();

        // Base files tokens substituted.
        File.Exists(Path.Combine(root, "src/MyApp.Api/MyApp.Api.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(root, "src/MyApp.Domain/MyApp.Domain.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(root, "MyApp.sln")).Should().BeTrue();

        // Platform bundles copied.
        File.Exists(Path.Combine(root, "docker-compose.yml")).Should().BeTrue();
        File.Exists(Path.Combine(root, ".devcontainer/devcontainer.json")).Should().BeTrue();
        File.Exists(Path.Combine(root, ".claude/CLAUDE.md")).Should().BeTrue();
        File.Exists(Path.Combine(root, ".claude/CLAUDE.md.template")).Should().BeFalse();

        var claudeMd = File.ReadAllText(Path.Combine(root, ".claude/CLAUDE.md"));
        claudeMd.Should().Contain("MyApp").And.Contain("postgres").And.Contain("ADR");
        File.Exists(Path.Combine(root, ".claude/skills/add-endpoint.md")).Should().BeTrue();

        // postgres capability contributed files.
        File.Exists(Path.Combine(root, "src/MyApp.Infrastructure/Persistence/ApplicationDbContext.cs")).Should().BeTrue();

        // Injector applied (postgres adds connection string to appsettings).
        var appsettings = File.ReadAllText(Path.Combine(root, "src/MyApp.Api/appsettings.json"));
        appsettings.Should().Contain("ConnectionStrings");
        appsettings.Should().Contain("Host=localhost;Database=app");

        // Injector applied (auth registers middleware).
        var program = File.ReadAllText(Path.Combine(root, "src/MyApp.Api/Program.cs"));
        program.Should().Contain("app.UseAuthentication();");

        // Injector applied (otel adds OTel packages to API csproj).
        var apiCsproj = File.ReadAllText(Path.Combine(root, "src/MyApp.Api/MyApp.Api.csproj"));
        apiCsproj.Should().Contain("OpenTelemetry.Extensions.Hosting");
        apiCsproj.Should().Contain("Microsoft.AspNetCore.Authentication.JwtBearer");

        // Infrastructure DI now calls postgres.
        var infraDi = File.ReadAllText(Path.Combine(root, "src/MyApp.Infrastructure/DependencyInjection.cs"));
        infraDi.Should().Contain("services.AddPostgres(config);");
    }

    [Fact]
    public async Task Writes_manifest_with_dependency_ordered_capabilities()
    {
        var planner = new Planner(
            name: "demo",
            multiService: false,
            capabilities: ["postgres"],
            deployTarget: "fly",
            includeClaude: false);

        await planner.RunAsync();

        var manifest = Manifest.Load(Path.Combine(_sandbox, "demo"));
        manifest.Name.Should().Be("demo");
        manifest.Capabilities.Should().StartWith("base"); // implicit dependency
        manifest.Capabilities.Should().Contain("postgres");
        manifest.Deploy.Should().Be("fly");
    }

    [Fact]
    public async Task Does_not_include_claude_bundle_when_opted_out()
    {
        var planner = new Planner(
            name: "no-claude",
            multiService: false,
            capabilities: [],
            deployTarget: "none",
            includeClaude: false);

        await planner.RunAsync();

        Directory.Exists(Path.Combine(_sandbox, "no-claude", ".claude")).Should().BeFalse();
    }
}
