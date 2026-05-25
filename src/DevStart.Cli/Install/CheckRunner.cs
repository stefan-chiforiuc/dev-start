using System.Diagnostics;
using System.Net.Sockets;

namespace DevStart.Install;

public enum CheckOutcome { Ok, Failed, Unknown }

public sealed record CheckResult(
    Capability.DoctorCheck Source,
    CheckOutcome Outcome,
    string Display);

/// Shared probe logic used by both `doctor` and `install`. Identical behavior
/// to the previous `DoctorCommand` implementation; only the return shape
/// changed (markup string + structured outcome instead of raw markup).
public static class CheckRunner
{
    public static async Task<CheckResult> RunAsync(Capability.DoctorCheck check, string projectRoot)
    {
        try
        {
            return check.Check switch
            {
                "service" when check.Port is int port => await ProbePort("localhost", port, check),
                "env" when check.Name is string key => ProbeEnv(key, check),
                "file" when check.Path is string rel => ProbeFile(projectRoot, rel, check),
                "dotnet-version" => ProbeTool("dotnet", "--version", check),
                "dotnet-tool" when check.Name is string tool => ProbeDotnetTool(tool, check),
                "tool" when check.Name is string tool => ProbeTool(tool, check.Args ?? "--version", check),
                _ => new CheckResult(check, CheckOutcome.Unknown, "[grey]unknown check[/]"),
            };
        }
        catch (Exception ex)
        {
            return new CheckResult(check, CheckOutcome.Failed, $"[red]error[/] {Escape(ex.Message)}");
        }
    }

    /// Quickly probe a list of checks. Used by `add` to surface "you need to
    /// run `devstart install`" hints without spinning the full doctor table.
    public static async Task<IReadOnlyList<CheckResult>> QuickProbe(
        IEnumerable<Capability.DoctorCheck> checks, string projectRoot)
    {
        var results = new List<CheckResult>();
        foreach (var check in checks)
        {
            results.Add(await RunAsync(check, projectRoot));
        }
        return results;
    }

    private static async Task<CheckResult> ProbePort(string host, int port, Capability.DoctorCheck check)
    {
        using var client = new TcpClient();
        var task = client.ConnectAsync(host, port);
        var completed = await Task.WhenAny(task, Task.Delay(500));
        var ok = completed == task && client.Connected;
        return new CheckResult(check,
            ok ? CheckOutcome.Ok : CheckOutcome.Failed,
            ok ? "[green]listening[/]" : "[red]unreachable[/]");
    }

    private static CheckResult ProbeEnv(string key, Capability.DoctorCheck check)
    {
        var ok = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key));
        return new CheckResult(check,
            ok ? CheckOutcome.Ok : CheckOutcome.Failed,
            ok ? "[green]set[/]" : "[red]missing[/]");
    }

    private static CheckResult ProbeFile(string projectRoot, string rel, Capability.DoctorCheck check)
    {
        var ok = File.Exists(Path.Join(projectRoot, rel));
        return new CheckResult(check,
            ok ? CheckOutcome.Ok : CheckOutcome.Failed,
            ok ? "[green]ok[/]" : "[red]missing[/]");
    }

    private static CheckResult ProbeTool(string tool, string args, Capability.DoctorCheck check)
    {
        try
        {
            var psi = new ProcessStartInfo(tool, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi);
            if (p is null) return new CheckResult(check, CheckOutcome.Failed, "[red]missing[/]");
            var output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            if (p.ExitCode != 0)
                return new CheckResult(check, CheckOutcome.Failed, "[red]error[/]");
            var first = output.Split('\n')[0];
            return new CheckResult(check, CheckOutcome.Ok, $"[green]{Escape(first)}[/]");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CheckResult(check, CheckOutcome.Failed, "[red]missing[/]");
        }
        catch (InvalidOperationException)
        {
            return new CheckResult(check, CheckOutcome.Failed, "[red]missing[/]");
        }
    }

    private static CheckResult ProbeDotnetTool(string toolName, Capability.DoctorCheck check)
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet", "tool list -g")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi);
            if (p is null) return new CheckResult(check, CheckOutcome.Failed, "[red]missing[/]");
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            var ok = output.Contains(toolName, StringComparison.OrdinalIgnoreCase);
            return new CheckResult(check,
                ok ? CheckOutcome.Ok : CheckOutcome.Failed,
                ok ? "[green]installed[/]" : "[red]not installed[/]");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CheckResult(check, CheckOutcome.Failed, "[red]missing[/]");
        }
        catch (InvalidOperationException)
        {
            return new CheckResult(check, CheckOutcome.Failed, "[red]missing[/]");
        }
    }

    internal static string Escape(string input) => input
        .Replace("[", "[[", StringComparison.Ordinal)
        .Replace("]", "]]", StringComparison.Ordinal);
}
