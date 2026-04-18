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
                    multiService: manifest.Services.Count > 1,
                    capabilities: manifest.Capabilities,
                    deployTarget: manifest.Deploy,
                    includeClaude: Directory.Exists(Path.Combine(root, ".claude")));
                var newBaselines = planner.Render(staging);

                var plan = BuildPlan(root, staging, oldBaselines, newBaselines);
                PrintPlan(plan);

                if (!apply)
                {
                    AnsiConsole.MarkupLine("");
                    AnsiConsole.MarkupLine("[grey]Dry run. Re-run with [cyan]--apply[/] to write changes.[/]");
                    return;
                }

                ApplyPlan(plan, root, staging);
                newBaselines.Save(root);
                AnsiConsole.MarkupLine("[green]Applied.[/]");
                if (plan.Conflicts.Count > 0)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]{plan.Conflicts.Count} conflict(s) written as *.upgrade-preview files — merge them manually.[/]");
                }
            }
            finally
            {
                try { Directory.Delete(staging, recursive: true); } catch { /* best-effort */ }
            }
        }, projectOpt, applyOpt);

        return cmd;
    }

    private sealed record UpgradePlan(
        List<string> Added,
        List<string> UpdatedCleanly,
        List<string> UnchangedOnBothSides,
        List<string> UserEditsPreserved,
        List<string> Conflicts,
        List<string> RemovedFromTemplate);

    private static UpgradePlan BuildPlan(
        string root, string staging, Baselines old, Baselines fresh)
    {
        var added = new List<string>();
        var updatedCleanly = new List<string>();
        var unchanged = new List<string>();
        var userPreserved = new List<string>();
        var conflicts = new List<string>();
        var removed = new List<string>();

        // Walk staging — every file the new template produces.
        foreach (var stagedAbs in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories))
        {
            var rel = Normalize(Path.GetRelativePath(staging, stagedAbs));
            var diskPath = Path.Combine(root, rel);
            var stagedBytes = File.ReadAllBytes(stagedAbs);
            var stagedHash = Baselines.Hash(stagedBytes);

            if (!File.Exists(diskPath))
            {
                added.Add(rel);
                continue;
            }

            var diskBytes = File.ReadAllBytes(diskPath);
            var diskHash = Baselines.Hash(diskBytes);

            if (diskHash == stagedHash)
            {
                unchanged.Add(rel);
                continue;
            }

            var baseHash = old.Get(rel);
            if (baseHash is null)
            {
                // We're generating a file the project already has but we
                // never baselined. Safest: treat as conflict.
                conflicts.Add(rel);
                continue;
            }

            if (diskHash == baseHash)
            {
                // User didn't touch this file; template moved forward.
                updatedCleanly.Add(rel);
            }
            else if (stagedHash == baseHash)
            {
                // Template unchanged for this file; user edited it. Keep.
                userPreserved.Add(rel);
            }
            else
            {
                // Both sides changed.
                conflicts.Add(rel);
            }
        }

        // Files that were in the old baseline but no longer in staging —
        // capability removed or template trimmed. We *don't* delete them;
        // the user might still want them. Just note.
        foreach (var key in old.Files.Keys)
        {
            var stagedAbs = Path.Combine(staging, key);
            if (!File.Exists(stagedAbs)) removed.Add(key);
        }

        return new UpgradePlan(added, updatedCleanly, unchanged, userPreserved, conflicts, removed);
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

    private static void ApplyPlan(UpgradePlan plan, string root, string staging)
    {
        foreach (var rel in plan.Added.Concat(plan.UpdatedCleanly))
        {
            var src = Path.Combine(staging, rel);
            var dst = Path.Combine(root, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(src, dst, overwrite: true);
        }

        foreach (var rel in plan.Conflicts)
        {
            var src = Path.Combine(staging, rel);
            var preview = Path.Combine(root, rel) + ".upgrade-preview";
            Directory.CreateDirectory(Path.GetDirectoryName(preview)!);
            File.Copy(src, preview, overwrite: true);
        }
    }

    private static string Normalize(string p) => p.Replace('\\', '/');
}
