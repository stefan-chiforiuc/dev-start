using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

public static class UpgradeCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var applyOpt = new Option<bool>("--apply",
            "Write the template refresh into the project. Files you haven't " +
            "edited get overwritten; files you've edited land as *.upgrade-preview " +
            "siblings for manual merge.");

        var cmd = new Command("upgrade",
            "Regenerate the project from the current templates and reconcile with your edits.")
        {
            projectOpt, applyOpt,
        };

        cmd.SetHandler((projectPath, apply) =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);
            var oldBaselines = Baselines.Load(root);

            AnsiConsole.MarkupLine($"[bold]dev-start upgrade[/] [grey]→[/] {root}");
            AnsiConsole.MarkupLine($"manifest templateVersion: [cyan]{manifest.TemplateVersion}[/]");

            var staging = Directory.CreateTempSubdirectory("devstart-upgrade-").FullName;
            AnsiConsole.MarkupLine($"[grey]staging:[/] {staging}");

            try
            {
                // Render the current template into staging the same way `new`
                // would — so the staging tree is a deterministic target.
                var planner = new Planner(
                    manifest.Name,
                    // Multi-service layouts are marked by having "gateway" in
                    // Services. Monoliths have just "api" (optionally "web"
                    // when the frontend capability is installed).
                    multiService: manifest.Services.Contains("gateway"),
                    capabilities: manifest.Capabilities,
                    deployTarget: manifest.Deploy,
                    includeClaude: Directory.Exists(Path.Join(root, ".claude")),
                    stack: manifest.Stack);
                var newBaselines = planner.Render(staging);

                var plan = Upgrader.BuildPlan(root, staging, oldBaselines);
                PrintPlan(plan);

                if (!apply)
                {
                    AnsiConsole.MarkupLine("");
                    AnsiConsole.MarkupLine("[grey]Dry run. Re-run with [cyan]--apply[/] to write changes.[/]");
                    return;
                }

                Upgrader.ApplyPlan(plan, root, staging);
                newBaselines.Save(root);

                // Bump the manifest's templateVersion to the CLI that
                // just applied the refresh — the project now reflects
                // that version's opinions.
                var oldVersion = manifest.TemplateVersion;
                manifest.TemplateVersion = CliVersion.Current;
                manifest.Save(root);

                AnsiConsole.MarkupLine(
                    $"[green]Applied.[/] templateVersion: [grey]{oldVersion}[/] → [cyan]{manifest.TemplateVersion}[/]");
                if (plan.Conflicts.Count > 0)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]{plan.Conflicts.Count} conflict(s) written as *.upgrade-preview files — merge them manually.[/]");
                }
            }
            finally
            {
                try { Directory.Delete(staging, recursive: true); }
                catch (IOException) { /* best-effort cleanup */ }
                catch (UnauthorizedAccessException) { /* best-effort cleanup */ }
            }
        }, projectOpt, applyOpt);

        return cmd;
    }

    private static void PrintPlan(UpgradePlan plan)
    {
        AnsiConsole.MarkupLine("");
        AnsiConsole.Write(new Rule("plan"));
        Print("[green]add[/]",         plan.Added);
        Print("[cyan]update[/]",       plan.UpdatedCleanly);
        Print("[grey]unchanged[/]",    plan.UnchangedOnBothSides);
        Print("[yellow]keep (user)[/]", plan.UserEditsPreserved);
        Print("[red]conflict[/]",      plan.Conflicts);
        Print("[grey]dropped from template[/]", plan.RemovedFromTemplate);
    }

    private static void Print(string tag, List<string> items)
    {
        if (items.Count == 0) return;
        AnsiConsole.MarkupLine($"{tag} ({items.Count}):");
        foreach (var p in items.OrderBy(s => s, StringComparer.Ordinal))
        {
            AnsiConsole.MarkupLine($"  {p.EscapeMarkup()}");
        }
    }
}
