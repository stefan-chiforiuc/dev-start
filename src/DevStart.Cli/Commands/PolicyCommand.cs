using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

/// <summary>
/// <c>dev-start policy</c> — list / apply / remove / validate org-level
/// policy bundles. Bundles ship embedded under <c>policies/&lt;name&gt;/</c>
/// in the CLI and are additive: applying a policy copies its files and runs
/// its injectors (reusing <see cref="CapabilityInstaller"/>'s infrastructure).
/// </summary>
public static class PolicyCommand
{
    public static Command Build()
    {
        var cmd = new Command("policy", "Manage org-level policy bundles.");
        cmd.AddCommand(BuildList());
        cmd.AddCommand(BuildApply());
        cmd.AddCommand(BuildRemove());
        cmd.AddCommand(BuildValidate());
        return cmd;
    }

    private static Command BuildList()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Project path (for installed markers).");
        var list = new Command("list", "List available policy bundles.") { projectOpt };
        list.SetHandler((projectPath) =>
        {
            List<string>? installed = null;
            try { installed = Manifest.Load(Path.GetFullPath(projectPath)).Policies; }
            catch { /* not in a project; just list */ }

            var table = new Table().AddColumns("Name", "Version", "Stacks", "Status");
            foreach (var name in Policy.AvailableNames())
            {
                Policy p;
                try { p = Policy.LoadEmbedded(name); }
                catch { continue; }
                var installedMark = installed is not null && installed.Contains(name)
                    ? "[green]installed[/]"
                    : "[grey]-[/]";
                var stacks = p.AppliesToStacks.Count == 0 ? "any" : string.Join(",", p.AppliesToStacks);
                table.AddRow(p.Name, p.Version, stacks, installedMark);
            }
            AnsiConsole.Write(table);
        }, projectOpt);
        return list;
    }

    private static Command BuildApply()
    {
        var nameArg = new Argument<string>("name", "Policy bundle to apply.");
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var apply = new Command("apply", "Apply a policy bundle to the project.") { nameArg, projectOpt };
        apply.SetHandler((name, projectPath) =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);
            var tokens = new Tokens(manifest.Name);
            var baselines = Baselines.Load(root);

            Policy policy;
            try { policy = Policy.LoadEmbedded(name); }
            catch (InvalidOperationException)
            {
                AnsiConsole.MarkupLine($"[red]Unknown policy[/] '{name}'. Run [cyan]dev-start policy list[/].");
                return;
            }

            if (policy.AppliesToStacks.Count > 0 &&
                !policy.AppliesToStacks.Contains(manifest.Stack, StringComparer.Ordinal))
            {
                AnsiConsole.MarkupLine(
                    $"[red]Stack mismatch[/]: [cyan]{name}[/] applies to " +
                    $"[yellow]{string.Join(",", policy.AppliesToStacks)}[/]; " +
                    $"project is [yellow]{manifest.Stack}[/].");
                return;
            }

            AnsiConsole.MarkupLine($"[bold]dev-start policy apply[/] [cyan]{name}[/]");
            AnsiConsole.MarkupLine($"[grey]{policy.Description}[/]");

            // Resolve extends transitively (depth-first, dedup by name). Each
            // policy in the chain contributes files + injectors in extends-first
            // order so the applying policy wins on overlapping markers.
            foreach (var p in ResolveExtends(policy))
            {
                CopyFiles(p, root, tokens, baselines);
                CapabilityInstaller.ApplyInjectors(
                    p.Injectors,
                    root,
                    tokens,
                    baselines,
                    fragmentReader: frag => Policy.ReadFragment(p.Name, frag));
            }

            if (!manifest.Policies.Contains(name)) manifest.Policies.Add(name);
            manifest.Save(root);
            baselines.Save(root);
            AnsiConsole.MarkupLine("[green]Applied.[/]");
        }, nameArg, projectOpt);
        return apply;
    }

    /// <summary>
    /// Flatten a policy's <c>extends</c> chain into a list ordered bases-first,
    /// applying policy last. Cycles are broken by name-dedup.
    /// </summary>
    internal static IReadOnlyList<Policy> ResolveExtends(Policy leaf)
    {
        var ordered = new List<Policy>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Visit(Policy p)
        {
            if (!seen.Add(p.Name)) return;
            foreach (var baseName in p.Extends)
            {
                Policy @base;
                try { @base = Policy.LoadEmbedded(baseName); }
                catch
                {
                    AnsiConsole.MarkupLine($"  [yellow]skip extends[/] — missing policy '{baseName}'");
                    continue;
                }
                Visit(@base);
            }
            ordered.Add(p);
        }

        Visit(leaf);
        return ordered;
    }

    private static void CopyFiles(Policy policy, string root, Tokens tokens, Baselines baselines)
    {
        foreach (var rel in Policy.FilesFor(policy.Name))
        {
            var bytes = Policy.ReadFile(policy.Name, rel)
                ?? throw new InvalidOperationException($"Missing policy file {policy.Name}/{rel}");
            var applied = tokens.Apply(rel);
            var dest = Path.Join(root, applied);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var text = System.Text.Encoding.UTF8.GetString(bytes);
            var content = System.Text.Encoding.UTF8.GetBytes(tokens.Apply(text));
            if (File.Exists(dest))
            {
                if (File.ReadAllBytes(dest).AsSpan().SequenceEqual(content))
                {
                    baselines.Record(applied, content);
                    continue;
                }
                AnsiConsole.MarkupLine(
                    $"  [yellow]skip[/] {rel.EscapeMarkup()} — already exists and differs; remove to re-apply.");
                continue;
            }
            File.WriteAllBytes(dest, content);
            baselines.Record(applied, content);
        }
    }

    private static Command BuildRemove()
    {
        var nameArg = new Argument<string>("name", "Policy bundle to remove from the manifest.");
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var remove = new Command("remove",
            "Remove a policy from the manifest. Does NOT reverse injected fragments — prints affected files for manual cleanup.")
        {
            nameArg, projectOpt,
        };
        remove.SetHandler((name, projectPath) =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);
            if (!manifest.Policies.Remove(name))
            {
                AnsiConsole.MarkupLine($"[yellow]{name}[/] is not installed.");
                return;
            }
            manifest.Save(root);

            try
            {
                var policy = Policy.LoadEmbedded(name);
                if (policy.Injectors.Count > 0)
                {
                    AnsiConsole.MarkupLine(
                        "[yellow]Manual cleanup may be required[/] — these files received injected fragments:");
                    foreach (var inj in policy.Injectors.Select(i => i.File).Distinct())
                    {
                        AnsiConsole.MarkupLine($"  [grey]-[/] {inj.EscapeMarkup()}");
                    }
                }
            }
            catch { /* missing policy — manifest still updated */ }

            AnsiConsole.MarkupLine("[green]Removed from manifest.[/]");
        }, nameArg, projectOpt);
        return remove;
    }

    private static Command BuildValidate()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var validate = new Command("validate",
            "Run installed policies' validators. Exits non-zero on any failure.")
        {
            projectOpt,
        };
        validate.SetHandler((projectPath) =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);
            if (manifest.Policies.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No policies installed.[/]");
                return;
            }

            var table = new Table().AddColumns("Policy", "Validator", "Result", "Detail");
            var failed = false;
            foreach (var name in manifest.Policies)
            {
                Policy p;
                try { p = Policy.LoadEmbedded(name); }
                catch
                {
                    table.AddRow(name, "-", "[red]missing[/]", "policy bundle not shipped with this CLI");
                    failed = true;
                    continue;
                }

                // Validate across the full extends chain so inherited
                // validators fire even when only the leaf is listed in the
                // manifest's Policies array.
                foreach (var link in ResolveExtends(p))
                {
                    foreach (var res in PolicyValidatorRunner.Run(link, root))
                    {
                        var tag = res.Passed ? "[green]ok[/]" : "[red]fail[/]";
                        table.AddRow(res.PolicyName, res.ValidatorId, tag, res.Message.EscapeMarkup());
                        if (!res.Passed) failed = true;
                    }
                }
            }
            AnsiConsole.Write(table);

            if (failed) Environment.ExitCode = 1;
        }, projectOpt);
        return validate;
    }
}
