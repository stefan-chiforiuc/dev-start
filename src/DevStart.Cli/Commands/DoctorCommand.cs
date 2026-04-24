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
        var fixOpt = new Option<bool>("--fix",
            "Best-effort remediate what can be fixed safely: write missing env keys " +
            "to .env.local using each capability's example values. Never overwrites existing " +
            "entries and never modifies .env (which may be gitignored differently).");

        var cmd = new Command("doctor",
            "Diagnose a project for drift, missing env vars, broken services, and missing tools.")
        {
            projectOpt, fixOpt,
        };

        cmd.SetHandler(async (projectPath, fix) =>
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
            table.AddRow("project", ".devstart.json", File.Exists(Path.Join(root, ".devstart.json"))
                ? "[green]ok[/]"
                : "[red]missing[/]");

            // Per-capability doctor checks + collect missing env keys for --fix.
            var missingEnv = new List<Capability.EnvAddition>();
            foreach (var capName in manifest.Capabilities)
            {
                var cap = Capability.LoadEmbedded(capName);
                foreach (var check in cap.Doctor)
                {
                    var result = await RunCheckAsync(check, root);
                    table.AddRow(capName, $"{check.Check} {check.Name ?? check.Path ?? ""}", result);

                    // If this was an env check that failed, see if the capability
                    // declared a known-example for it.
                    if (check.Check == "env" && check.Name is string envKey
                        && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envKey)))
                    {
                        var hint = cap.EnvAdditions.FirstOrDefault(e =>
                            string.Equals(e.Key, envKey, StringComparison.Ordinal));
                        if (hint is not null) missingEnv.Add(hint);
                    }
                }
            }

            // Policy validators run informationally (doctor never fails).
            // Walk extends so inherited bundles' validators show up too.
            foreach (var policyName in manifest.Policies)
            {
                Policy policy;
                try { policy = Policy.LoadEmbedded(policyName); }
                catch
                {
                    table.AddRow("policy", policyName, "[yellow]missing bundle[/]");
                    continue;
                }
                foreach (var link in PolicyCommand.ResolveExtends(policy))
                {
                    foreach (var res in PolicyValidatorRunner.Run(link, root))
                    {
                        table.AddRow($"policy/{res.PolicyName}", res.ValidatorId,
                            res.Passed ? "[green]ok[/]" : $"[red]fail[/] {res.Message}");
                    }
                }
            }

            AnsiConsole.Write(table);

            if (fix)
            {
                ApplyFixes(root, missingEnv);
            }
            else if (missingEnv.Count > 0)
            {
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine(
                    $"[grey]{missingEnv.Count} missing env key(s) can be auto-populated with[/] [cyan]dev-start doctor --fix[/].");
            }
        }, projectOpt, fixOpt);

        return cmd;
    }

    private static void ApplyFixes(string projectRoot, IReadOnlyList<Capability.EnvAddition> missing)
    {
        if (missing.Count == 0)
        {
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[green]Nothing to fix.[/]");
            return;
        }

        var envFile = Path.Join(projectRoot, ".env.local");
        var existing = File.Exists(envFile) ? File.ReadAllText(envFile) : "";

        using var writer = new StreamWriter(envFile, append: true);
        if (existing.Length > 0 && !existing.EndsWith('\n'))
        {
            writer.WriteLine();
        }
        if (!existing.Contains("# dev-start doctor --fix", StringComparison.Ordinal))
        {
            writer.WriteLine("# dev-start doctor --fix — placeholder values from capability examples");
            writer.WriteLine("# Review each entry; examples point at local-dev defaults, not real secrets.");
        }

        var wrote = 0;
        foreach (var env in missing)
        {
            // Skip if the user already wrote this key, even as a comment.
            if (existing.Contains($"{env.Key}=", StringComparison.Ordinal)) continue;
            writer.WriteLine($"{env.Key}={env.Example}");
            wrote++;
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine(
            $"[green]doctor --fix[/] wrote [cyan]{wrote}[/] entries to [grey]{Path.GetRelativePath(projectRoot, envFile)}[/]. " +
            "Review before running [cyan]just up[/].");
    }

    private static async Task<string> RunCheckAsync(Capability.DoctorCheck check, string projectRoot)
    {
        try
        {
            return check.Check switch
            {
                "service" when check.Port is int port => await CheckPortAsync("localhost", port),
                "env" when check.Name is string key => CheckEnv(key),
                "file" when check.Path is string rel => File.Exists(Path.Join(projectRoot, rel))
                    ? "[green]ok[/]"
                    : "[red]missing[/]",
                "dotnet-version" => ToolVersion("dotnet", "--version"),
                "dotnet-tool" when check.Name is string tool => CheckDotnetTool(tool),
                "tool" when check.Name is string tool => ToolVersion(tool, check.Args ?? "--version"),
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
        catch (System.ComponentModel.Win32Exception)
        {
            return "[red]missing[/]";
        }
        catch (InvalidOperationException)
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
        catch (System.ComponentModel.Win32Exception)
        {
            return "[red]missing[/]";
        }
        catch (InvalidOperationException)
        {
            return "[red]missing[/]";
        }
    }

    private static string Escape(string input) => input
        .Replace("[", "[[", StringComparison.Ordinal)
        .Replace("]", "]]", StringComparison.Ordinal);
}
