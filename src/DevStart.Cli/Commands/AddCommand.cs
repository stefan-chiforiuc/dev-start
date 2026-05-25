using System.CommandLine;
using DevStart.Install;
using Spectre.Console;

namespace DevStart.Commands;

public static class AddCommand
{
    public static Command Build()
    {
        var capArg = new Argument<string>("capability", "Capability to add (e.g. auth, cache, queue, s3, deploy).");
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var targetOpt = new Option<string?>("--target",
            "For `deploy`: which target to install (e.g. fly, aca).");
        var engineOpt = new Option<string?>("--engine",
            "For engine-swappable families like `cache`: which engine (e.g. redis, memory).");
        var frameworkOpt = new Option<string?>("--framework",
            "For `backend` / `frontend`: which framework variant (e.g. aspnet, fastify, react).");
        var versionOpt = new Option<string?>("--framework-version",
            "For `backend` / `frontend`: which framework version (e.g. 8, 9, 19).");

        var cmd = new Command("add", "Add a capability to an existing project.")
        {
            capArg, projectOpt, targetOpt, engineOpt, frameworkOpt, versionOpt,
        };

        cmd.SetHandler(async ctx =>
        {
            var capName = ctx.ParseResult.GetValueForArgument(capArg);
            var projectPath = ctx.ParseResult.GetValueForOption(projectOpt) ?? ".";
            var target = ctx.ParseResult.GetValueForOption(targetOpt);
            var engine = ctx.ParseResult.GetValueForOption(engineOpt);
            var framework = ctx.ParseResult.GetValueForOption(frameworkOpt);
            var version = ctx.ParseResult.GetValueForOption(versionOpt);

            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);

            // Resolve the user-typed name to a concrete capability folder.
            // The resolver normalizes flat names (`auth` → `ts-auth` in a TS
            // project) and family selections (`deploy --target fly` →
            // `deploy-fly`, `cache --engine memory` → `cache-memory`).
            // `--engine` and `--target` are surface-level aliases for the
            // same underlying "pick a variant inside this family" concept.
            var familyTarget = engine ?? target ?? framework;
            if (capName.Equals("deploy", StringComparison.Ordinal))
            {
                var resolvedDeploy = CapabilityResolver.ResolveDeploy(manifest.Stack, target ?? "");
                if (resolvedDeploy is null)
                {
                    AnsiConsole.MarkupLine(
                        "[red]deploy requires --target[/]: pass [cyan]--target fly[/] or [cyan]--target aca[/].");
                    return;
                }
                capName = resolvedDeploy;
            }
            else
            {
                var resolved = CapabilityResolver.Resolve(new CapabilityResolver.Selection(
                    capName, manifest.Stack, familyTarget, version));
                if (resolved is null)
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Unknown capability[/] [cyan]{capName}[/] for stack [yellow]{manifest.Stack}[/].");
                    return;
                }
                if (!resolved.Equals(capName, StringComparison.Ordinal))
                {
                    AnsiConsole.MarkupLine($"[grey]resolved {capName} → {resolved}[/]");
                    capName = resolved;
                }
            }

            if (manifest.Capabilities.Contains(capName, StringComparer.Ordinal))
            {
                AnsiConsole.MarkupLine($"[yellow]{capName}[/] is already installed. Use [cyan]dev-start upgrade[/] to refresh.");
                return;
            }

            var cap = Capability.LoadEmbedded(capName);

            // Stack gate: explicit capability.stacks wins. Fall back to prefix
            // convention — ts-* is typescript-only, otherwise dotnet-only —
            // except stack-agnostic capabilities that declare dependsOnByStack.
            if (cap.Stacks.Count > 0 && !cap.Stacks.Contains(manifest.Stack, StringComparer.Ordinal))
            {
                AnsiConsole.MarkupLine(
                    $"[red]Stack mismatch[/]: [cyan]{capName}[/] targets [yellow]{string.Join(", ", cap.Stacks)}[/]; " +
                    $"this project is [yellow]{manifest.Stack}[/].");
                return;
            }
            if (cap.Stacks.Count == 0 && cap.DependsOnByStack is null)
            {
                var implied = capName.StartsWith("ts-", StringComparison.Ordinal)
                    ? Planner.StackTypescript
                    : Planner.StackDotnet;
                if (implied != manifest.Stack)
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Stack mismatch[/]: [cyan]{capName}[/] is a [yellow]{implied}[/] capability; " +
                        $"this project is [yellow]{manifest.Stack}[/].");
                    return;
                }
            }

            // Dependency check honors the alias map so `dependsOn: ["base"]`
            // is satisfied by `base-aspnet-9` or `ts-base`.
            var aliases = CapabilityResolver.BuildAliasMap(manifest.Capabilities);
            foreach (var dep in cap.EffectiveDependsOn(manifest.Stack))
            {
                var concreteDep = CapabilityResolver.ApplyAliases(dep, aliases);
                if (!manifest.Capabilities.Contains(concreteDep, StringComparer.Ordinal)
                    && !manifest.Capabilities.Contains(dep, StringComparer.Ordinal))
                {
                    AnsiConsole.MarkupLine($"[red]Missing dependency[/]: [cyan]{capName}[/] requires [cyan]{dep}[/]. Install it first.");
                    return;
                }
            }

            foreach (var conflict in cap.ConflictsWith)
            {
                if (manifest.Capabilities.Contains(conflict, StringComparer.Ordinal))
                {
                    AnsiConsole.MarkupLine($"[red]Conflict[/]: [cyan]{capName}[/] conflicts with [cyan]{conflict}[/] which is installed.");
                    return;
                }
            }

            AnsiConsole.MarkupLine($"[bold]dev-start add[/] [cyan]{capName}[/] [grey]→[/] {root}");
            AnsiConsole.MarkupLine($"[grey]{cap.Description}[/]");

            var tokens = new Tokens(manifest.Name);
            var baselines = Baselines.Load(root);
            CapabilityInstaller.Install(capName, root, tokens, baselines);

            manifest.Capabilities.Add(capName);
            if (capName == "frontend" && !manifest.Services.Contains("web"))
            {
                manifest.Services.Add("web");
            }
            if (cap.Family == "deploy" && cap.Framework is not null)
            {
                manifest.Deploy = cap.Framework;
            }
            manifest.Save(root);
            baselines.Save(root);

            AnsiConsole.MarkupLine($"[green]Installed.[/]");

            // Probe just-added capability's prereqs so the user knows whether
            // a `devstart install` run is needed before `just up`.
            if (cap.Doctor.Count > 0)
            {
                var probe = await CheckRunner.QuickProbe(cap.Doctor, root);
                var failing = probe.Count(r => r.Outcome == CheckOutcome.Failed);
                if (failing > 0)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]{failing} new prerequisite(s) detected.[/] Run [cyan]dev-start install[/] " +
                        "to install only what's missing ([cyan]--dry-run[/] to preview first).");
                }
            }

            if (cap.PostInstall.Count > 0)
            {
                AnsiConsole.MarkupLine("[grey]post-install:[/]");
                foreach (var step in cap.PostInstall)
                {
                    AnsiConsole.MarkupLine($"  [grey]$[/] {step}");
                }
                AnsiConsole.MarkupLine("[grey]Run the above if you haven't already.[/]");
            }
        });

        return cmd;
    }
}
