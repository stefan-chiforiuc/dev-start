using System.CommandLine;
using System.Diagnostics;
using System.Net.Sockets;
using Spectre.Console;

namespace DevStart.Commands;

public static class DoctorCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var cmd = new Command("doctor", "Diagnose a project for drift, missing env vars, broken services, and missing tools.") { projectOpt };

        cmd.SetHandler(async (projectPath) =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);

            AnsiConsole.MarkupLine($"[bold]dev-start doctor[/] [grey]→[/] {root}");
            AnsiConsole.MarkupLine($"manifest: [cyan]{manifest.Name}[/] v[cyan]{manifest.TemplateVersion}[/]");
            AnsiConsole.MarkupLine($"capabilities: {string.Join(", ", manifest.Capabilities)}");
            AnsiConsole.WriteLine();

            var table = new Table().AddColumns("Category", "Check", "Result");

            // Baseline tool checks — independent of manifest.
            table.AddRow("tool", "git", ToolVersion("git", "--version"));
            table.AddRow("tool", "dotnet", ToolVersion("dotnet", "--version"));
            table.AddRow("tool", "docker", ToolVersion("docker", "--version"));
            table.AddRow("tool", "just", ToolVersion("just", "--version"));

            // Known expected manifest file.
            table.AddRow("project", ".devstart.json", File.Exists(Path.Combine(root, ".devstart.json"))
                ? "[green]ok[/]"
                : "[red]missing[/]");

            // Per-capability doctor checks.
            foreach (var capName in manifest.Capabilities)
            {
                var cap = Capability.LoadEmbedded(capName);
                foreach (var check in cap.Doctor)
                {
                    var result = await RunCheckAsync(check, root);
                    table.AddRow(capName, $"{check.Check} {check.Name ?? check.Path ?? ""}", result);
                }
            }

            AnsiConsole.Write(table);
        }, projectOpt);

        return cmd;
    }

    private static async Task<string> RunCheckAsync(Capability.DoctorCheck check, string projectRoot)
    {
        try
        {
            return check.Check switch
            {
                "service" when check.Port is int port => await CheckPortAsync("localhost", port),
                "env" when check.Name is string key => CheckEnv(key),
                "file" when check.Path is string rel => File.Exists(Path.Combine(projectRoot, rel))
                    ? "[green]ok[/]"
                    : "[red]missing[/]",
                "dotnet-version" => ToolVersion("dotnet", "--version"),
                "dotnet-tool" when check.Name is string tool => CheckDotnetTool(tool),
                _ => "[grey]unknown check[/]",
            };
        }
        catch (Exception ex)
        {
            return $"[red]error[/] {ex.Message}";
        }
    }

    private static async Task<string> CheckPortAsync(string host, int port)
    {
        using var client = new TcpClient();
        var task = client.ConnectAsync(host, port);
        var completed = await Task.WhenAny(task, Task.Delay(500));
        return completed == task && client.Connected ? "[green]listening[/]" : "[red]unreachable[/]";
    }

    private static string CheckEnv(string key)
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)) ? "[green]set[/]" : "[red]missing[/]";

    private static string ToolVersion(string tool, string args)
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
            if (p is null) return "[red]missing[/]";
            var output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            return p.ExitCode == 0
                ? $"[green]{Escape(output.Split('\n')[0])}[/]"
                : "[red]error[/]";
        }
        catch
        {
            return "[red]missing[/]";
        }
    }

    private static string CheckDotnetTool(string toolName)
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
            if (p is null) return "[red]missing[/]";
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output.Contains(toolName, StringComparison.OrdinalIgnoreCase)
                ? "[green]installed[/]"
                : "[red]not installed[/]";
        }
        catch
        {
            return "[red]missing[/]";
        }
    }

    private static string Escape(string input) => input
        .Replace("[", "[[", StringComparison.Ordinal)
        .Replace("]", "]]", StringComparison.Ordinal);
}
