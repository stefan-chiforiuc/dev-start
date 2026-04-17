using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

public static class UpgradeCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var dryOpt = new Option<bool>("--dry-run", "Show planned changes without writing.");

        var cmd = new Command("upgrade", "Diff the project against the latest template versions and apply updates.")
        {
            projectOpt, dryOpt
        };

        cmd.SetHandler((projectPath, dryRun) =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);

            AnsiConsole.MarkupLine($"[bold]dev-start upgrade[/] [grey]→[/] {root}");
            AnsiConsole.MarkupLine($"current template version: [cyan]{manifest.TemplateVersion}[/]");

            // TODO v0.2:
            //   1. Resolve latest capability versions from embedded resources.
            //   2. For each installed capability, diff `files/` + `patches/` against the target tree.
            //   3. Produce a unified patch file.
            //   4. If --dry-run, print the patch. Otherwise apply it via `git apply --3way` on a
            //      branch, commit, and leave the branch for the user to PR.

            AnsiConsole.MarkupLine("[yellow]Upgrade is stubbed in v0.1.[/] Tracking: issue #TODO.");
            if (dryRun) AnsiConsole.MarkupLine("[grey](dry-run mode requested)[/]");
        }, projectOpt, dryOpt);

        return cmd;
    }
}
