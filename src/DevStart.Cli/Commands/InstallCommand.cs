using System.CommandLine;
using DevStart.Install;
using Spectre.Console;

namespace DevStart.Commands;

public static class InstallCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var dryRunOpt = new Option<bool>("--dry-run", "Preview the install plan without changes.");
        var yesOpt = new Option<bool>("--yes", "Skip the confirmation prompt.");
        var skipServicesOpt = new Option<bool>("--skip-services", "Don't start docker-compose services.");
        var includeOptionalOpt = new Option<bool>("--include-optional",
            "Also install tools tagged `required: false` (e.g. flyctl, az).");
        var onlyOpt = new Option<string?>("--only",
            "Filter actions: runtimes | tools | services | dotnet-tools | all (default).");

        var cmd = new Command("install",
            "Install host prerequisites required by this project's capabilities.")
        {
            projectOpt, dryRunOpt, yesOpt, skipServicesOpt, includeOptionalOpt, onlyOpt,
        };

        cmd.SetHandler(async ctx =>
        {
            var projectPath = ctx.ParseResult.GetValueForOption(projectOpt) ?? ".";
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            var yes = ctx.ParseResult.GetValueForOption(yesOpt);
            var skipServices = ctx.ParseResult.GetValueForOption(skipServicesOpt);
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var only = ctx.ParseResult.GetValueForOption(onlyOpt);
            ctx.ExitCode = await Run(projectPath, dryRun, yes, skipServices, includeOptional, only);
        });

        return cmd;
    }

    private static async Task<int> Run(
        string projectPath, bool dryRun, bool yes, bool skipServices,
        bool includeOptional, string? only)
    {
        var root = Path.GetFullPath(projectPath);
        var manifest = Manifest.Load(root);

        AnsiConsole.MarkupLine($"[bold]dev-start install[/] [grey]→[/] {root}");
        AnsiConsole.MarkupLine($"manifest: [cyan]{manifest.Name}[/] capabilities: {string.Join(", ", manifest.Capabilities)}");

        var os = OsProbe.Detect();
        var pkg = PackageManagerFactory.Detect(os);
        AnsiConsole.MarkupLine($"host: [yellow]{os.PrettyName}[/] | package manager: [yellow]{pkg.Name}[/]");
        if (pkg is NullPackageManager)
        {
            AnsiConsole.MarkupLine(
                "[yellow]No supported package manager detected.[/] " +
                "Actions will show manual URLs only.");
        }
        AnsiConsole.WriteLine();

        var checks = BuildCheckList(manifest, root);
        var results = await CheckRunner.QuickProbe(checks, root);

        var failing = results.Where(r => r.Outcome == CheckOutcome.Failed).ToList();
        if (failing.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All prerequisites are present.[/] Nothing to install.");
            return 0;
        }

        var actions = InstallPlan.Build(failing, pkg, includeOptional);
        actions = FilterByOnly(actions, only);
        if (skipServices)
        {
            actions = actions.Where(a => a.Category != ActionCategory.Service).ToList();
        }

        RenderPlan(actions);

        if (dryRun)
        {
            AnsiConsole.MarkupLine("[grey]--dry-run: nothing executed.[/]");
            return actions.Any(a => !a.Skipped) ? 1 : 0;
        }

        if (actions.All(a => a.Skipped))
        {
            AnsiConsole.MarkupLine("[yellow]All actions are skipped[/] (manual URLs above). Nothing to execute.");
            return 1;
        }

        if (!yes && !AnsiConsole.Confirm("Proceed with the actions above?"))
        {
            AnsiConsole.MarkupLine("[grey]aborted.[/]");
            return 1;
        }

        var runner = new DefaultProcessRunner();
        await ExecuteActions(actions, runner, root);

        // Recheck pass.
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Re-checking…[/]");
        var rechecks = await CheckRunner.QuickProbe(checks, root);
        var stillFailing = rechecks.Count(r => r.Outcome == CheckOutcome.Failed);

        AnsiConsole.WriteLine();
        if (stillFailing == 0)
        {
            AnsiConsole.MarkupLine("[green]All green.[/] Run [cyan]dev-start doctor[/] to verify.");
            return 0;
        }

        AnsiConsole.MarkupLine(
            $"[yellow]{stillFailing} check(s) still failing.[/] " +
            "Some installers need a fresh shell so PATH picks up new binaries — " +
            "open a new terminal and re-run [cyan]dev-start install[/], or run " +
            "[cyan]dev-start doctor[/] to see details.");
        return 1;
    }

    // Build the deduplicated set of checks for this project: baseline + every
    // capability's `Doctor[]`. Baseline is conservative — anything the
    // generated `just` recipes need to function.
    public static List<Capability.DoctorCheck> BuildCheckList(Manifest manifest, string projectRoot)
    {
        var list = new List<Capability.DoctorCheck>
        {
            new() { Check = "tool", Name = "git" },
            new() { Check = "tool", Name = "dotnet" },
            new() { Check = "tool", Name = "just" },
        };

        // bash is required wherever the generated justfile pins `set shell`.
        if (File.Exists(Path.Join(projectRoot, "justfile")))
        {
            list.Add(new() { Check = "tool", Name = "bash" });
        }

        var needsDocker = false;
        foreach (var capName in manifest.Capabilities)
        {
            Capability cap;
            try { cap = Capability.LoadEmbedded(capName); }
            catch (InvalidOperationException) { continue; }

            foreach (var check in cap.Doctor)
            {
                list.Add(check);
                if (check.Check == "service") needsDocker = true;
            }
            if (cap.AddsServices.Count > 0) needsDocker = true;
        }

        if (needsDocker || File.Exists(Path.Join(projectRoot, "docker-compose.yml"))
            || File.Exists(Path.Join(projectRoot, "platform", "compose", "docker-compose.yml")))
        {
            list.Insert(0, new() { Check = "tool", Name = "docker" });
        }

        return Deduplicate(list);
    }

    private static List<Capability.DoctorCheck> Deduplicate(List<Capability.DoctorCheck> input)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var output = new List<Capability.DoctorCheck>();
        foreach (var c in input)
        {
            var key = $"{c.Check}|{c.Name ?? c.Path ?? ""}|{c.Port?.ToString() ?? ""}";
            if (seen.Add(key)) output.Add(c);
        }
        return output;
    }

    private static List<InstallAction> FilterByOnly(List<InstallAction> actions, string? only)
    {
        if (string.IsNullOrEmpty(only) || only.Equals("all", StringComparison.OrdinalIgnoreCase))
            return actions;
        return only.ToLowerInvariant() switch
        {
            "runtimes" => actions.Where(a => a.Category == ActionCategory.Runtime).ToList(),
            "tools" => actions.Where(a => a.Category == ActionCategory.Tool).ToList(),
            "services" => actions.Where(a => a.Category == ActionCategory.Service).ToList(),
            "dotnet-tools" => actions.Where(a => a.Category == ActionCategory.DotnetTool).ToList(),
            _ => actions,
        };
    }

    private static void RenderPlan(IReadOnlyList<InstallAction> actions)
    {
        var table = new Table().AddColumns("Category", "Name", "Action");
        foreach (var a in actions)
        {
            var action = a.Skipped
                ? $"[grey]skip[/] — {CheckRunner.Escape(a.Reason)}{(a.ManualUrl is null ? "" : $" → [link]{a.ManualUrl}[/]")}"
                : $"[green]install[/] — {CheckRunner.Escape(a.Command ?? "(no command)")}";
            table.AddRow(a.Category.ToString().ToLowerInvariant(), CheckRunner.Escape(a.Name), action);
        }
        AnsiConsole.Write(table);
    }

    private static async Task ExecuteActions(
        IReadOnlyList<InstallAction> actions, IProcessRunner runner, string projectRoot)
    {
        var services = new List<InstallAction>();
        foreach (var action in actions.Where(a => !a.Skipped))
        {
            switch (action.Category)
            {
                case ActionCategory.Service:
                    services.Add(action); // batched at the end
                    continue;
                case ActionCategory.DotnetTool:
                    await ExecCommand(runner, action);
                    break;
                case ActionCategory.Runtime:
                case ActionCategory.Tool:
                    await ExecCommand(runner, action);
                    break;
            }
        }

        if (services.Count > 0)
        {
            AnsiConsole.MarkupLine($"[grey]→[/] starting {services.Count} service(s) via docker compose…");
            var (ok, msg) = await ServiceStarter.StartAsync(services, runner, projectRoot);
            AnsiConsole.MarkupLine(ok ? $"[green]✓[/] {CheckRunner.Escape(msg)}" : $"[red]✗[/] {CheckRunner.Escape(msg)}");
        }
    }

    private static async Task ExecCommand(IProcessRunner runner, InstallAction action)
    {
        if (action.Command is null) return;
        AnsiConsole.MarkupLine($"[grey]$[/] {CheckRunner.Escape(action.Command)}");

        // Split the shell-style command into argv. Good enough for the tightly
        // controlled set of commands the catalog produces (no quoting nightmares).
        var parts = action.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var file = parts[0];
        var args = string.Join(' ', parts.Skip(1));

        var result = await runner.RunAsync(file, args);
        if (result.ExitCode == 0)
        {
            AnsiConsole.MarkupLine($"  [green]✓[/] {CheckRunner.Escape(action.Name)}");
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"  [red]✗[/] {CheckRunner.Escape(action.Name)} — exit {result.ExitCode}: " +
                CheckRunner.Escape(result.StdErr.Trim().Split('\n').FirstOrDefault() ?? ""));
        }
    }
}
