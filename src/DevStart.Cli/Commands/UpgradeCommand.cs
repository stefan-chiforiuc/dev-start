using System.CommandLine;
using System.Diagnostics;
using Spectre.Console;

namespace DevStart.Commands;

public static class UpgradeCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");

        var cmd = new Command("upgrade", "Regenerate the project from the current templates into a staging dir, then diff.")
        {
            projectOpt,
        };

        cmd.SetHandler(projectPath =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);

            AnsiConsole.MarkupLine($"[bold]dev-start upgrade[/] [grey]→[/] {root}");
            AnsiConsole.MarkupLine($"manifest templateVersion: [cyan]{manifest.TemplateVersion}[/]");

            var staging = Directory.CreateTempSubdirectory("devstart-upgrade-").FullName;
            AnsiConsole.MarkupLine($"[grey]staging:[/] {staging}");

            try
            {
                var tokens = new Tokens(manifest.Name);
                foreach (var cap in manifest.Capabilities)
                {
                    AnsiConsole.MarkupLine($"[cyan]· regenerate[/] {cap}");
                    CapabilityInstaller.CopyFiles(cap, staging, tokens);
                }

                // Second pass: apply injectors now that all capability files exist.
                foreach (var cap in manifest.Capabilities)
                {
                    CapabilityInstaller.ApplyInjectors(cap, staging, tokens);
                }

                AnsiConsole.Write(new Rule("diff"));
                var output = DiffDirs(root, staging);
                AnsiConsole.WriteLine(output);

                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[grey]To apply a specific change, copy the relevant hunks manually.[/]");
                AnsiConsole.MarkupLine("[grey]Full 3-way merge + --apply land in v0.4 once baseline tracking is in place.[/]");
            }
            finally
            {
                try { Directory.Delete(staging, recursive: true); } catch { /* best-effort */ }
            }
        }, projectOpt);

        return cmd;
    }

    private static string DiffDirs(string a, string b)
    {
        var psi = new ProcessStartInfo("git", $"diff --no-index --no-color \"{a}\" \"{b}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        // `git diff --no-index` exits 1 when the two trees differ — that's normal.
        return stdout.Length > 0 ? stdout : $"No differences.\n{stderr}";
    }
}
