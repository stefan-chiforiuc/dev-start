using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace DevStart.Tests;

[Collection("SandboxCwd")]
public class McpDeclarativeTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;

    public McpDeclarativeTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-mcp-").FullName;
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
    public async Task McpJson_picks_up_postgres_and_otel_entries()
    {
        var planner = new Planner("mcp-demo", false, ["postgres", "otel"], "none", includeClaude: false);
        await planner.RunAsync();

        var mcpPath = Path.Join(_sandbox, "mcp-demo", ".mcp.json");
        File.Exists(mcpPath).Should().BeTrue();
        var json = JsonDocument.Parse(File.ReadAllText(mcpPath));
        var servers = json.RootElement.GetProperty("mcpServers");
        servers.TryGetProperty("postgres", out _).Should().BeTrue();
        servers.TryGetProperty("seq-logs", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Baseline_only_project_emits_empty_mcp_servers()
    {
        var planner = new Planner("mcp-empty", false, [], "none", includeClaude: false);
        await planner.RunAsync();

        var mcpPath = Path.Join(_sandbox, "mcp-empty", ".mcp.json");
        var json = JsonDocument.Parse(File.ReadAllText(mcpPath));
        json.RootElement.GetProperty("mcpServers").EnumerateObject().Should().BeEmpty();
    }
}
