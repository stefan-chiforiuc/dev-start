using System.CommandLine;
using DevStart.Commands;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Builds the root command tree exactly as Program.cs does and checks that
/// every subcommand resolves, every option has a valid default, and
/// --help renders without throwing. Catches broken wiring at test time
/// instead of first user invocation.
/// </summary>
[Collection("SandboxCwd")]
public class CliSmokeTests
{
    private static RootCommand BuildRoot()
    {
        var root = new RootCommand("dev-start — opinionated .NET scaffolder and day-to-day companion.");
        root.AddCommand(NewCommand.Build());
        root.AddCommand(AddCommand.Build());
        root.AddCommand(DoctorCommand.Build());
        root.AddCommand(UpgradeCommand.Build());
        root.AddCommand(ListCommand.Build());
        root.AddCommand(CapabilityCommand.Build());
        root.AddCommand(PromoteCommand.Build());
        root.AddCommand(PolicyCommand.Build());
        return root;
    }

    [Fact]
    public void Root_command_tree_builds_without_throwing()
    {
        var act = BuildRoot;
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("new")]
    [InlineData("add")]
    [InlineData("doctor")]
    [InlineData("upgrade")]
    [InlineData("list")]
    [InlineData("capability")]
    [InlineData("promote")]
    [InlineData("policy")]
    public void Subcommand_exists(string name)
    {
        var root = BuildRoot();
        root.Subcommands.Should().Contain(c => c.Name == name,
            because: $"Program.cs registers {name}");
    }

    [Fact]
    public void Capability_subcommand_has_new_child()
    {
        var root = BuildRoot();
        var cap = root.Subcommands.First(c => c.Name == "capability");
        cap.Subcommands.Should().Contain(c => c.Name == "new");
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("new --help")]
    [InlineData("add --help")]
    [InlineData("doctor --help")]
    [InlineData("upgrade --help")]
    [InlineData("list --help")]
    [InlineData("capability new --help")]
    [InlineData("promote --help")]
    [InlineData("policy --help")]
    [InlineData("policy list --help")]
    [InlineData("policy apply --help")]
    [InlineData("policy validate --help")]
    public async Task Help_renders_without_throwing(string argline)
    {
        // InlineData can't carry string[] directly (attribute-argument
        // rules), so we pass the command line as a single string and
        // split it here.
        var args = argline.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var root = BuildRoot();
        var exit = await root.InvokeAsync(args);
        exit.Should().Be(0, because: $"`dev-start {argline}` must at least render help");
    }

    [Fact]
    public async Task List_without_args_runs()
    {
        var root = BuildRoot();
        // Running `list` in a dir with no manifest should still work (plain list).
        using var tmp = new TempCwd();
        var exit = await root.InvokeAsync(["list"]);
        exit.Should().Be(0);
    }

    private sealed class TempCwd : IDisposable
    {
        private readonly string _prior;
        private readonly string _tmp;

        public TempCwd()
        {
            _prior = Directory.GetCurrentDirectory();
            _tmp = Directory.CreateTempSubdirectory("devstart-cli-smoke-").FullName;
            Directory.SetCurrentDirectory(_tmp);
        }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(_prior);
            try { Directory.Delete(_tmp, recursive: true); } catch { /* best-effort */ }
        }
    }
}
