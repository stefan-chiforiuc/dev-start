using System.CommandLine;
using DevStart.Install;
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

        cmd.SetHandler(async ctx =>
        {
            var projectPath = ctx.ParseResult.GetValueForOption(projectOpt) ?? ".";
            var fix = ctx.ParseResult.GetValueForOption(fixOpt);
            ctx.ExitCode = await Run(projectPath, fix);
        });

        return cmd;
    }

    private static async Task<int> Run(string projectPath, bool fix)
    {
        var root = Path.GetFullPath(projectPath);
        var manifest = Manifest.Load(root);

        AnsiConsole.MarkupLine($"[bold]dev-start doctor[/] [grey]→[/] {root}");
        AnsiConsole.MarkupLine($"manifest: [cyan]{manifest.Name}[/] v[cyan]{manifest.TemplateVersion}[/]");
        AnsiConsole.MarkupLine($"capabilities: {string.Join(", ", manifest.Capabilities)}");
        AnsiConsole.WriteLine();

        var table = new Table().AddColumns("Category", "Check", "Result");
        var anyFailed = false;

        // Baseline tool checks — independent of manifest. Reuse CheckRunner so
        // doctor and install stay in lockstep.
        var baseline = new (string label, Capability.DoctorCheck check)[]
        {
            ("tool", new() { Check = "tool", Name = "git" }),
            ("tool", new() { Check = "tool", Name = "dotnet" }),
            ("tool", new() { Check = "tool", Name = "docker" }),
            ("tool", new() { Check = "tool", Name = "just" }),
        };
        foreach (var (label, check) in baseline)
        {
            var result = await CheckRunner.RunAsync(check, root);
            if (result.Outcome == CheckOutcome.Failed) anyFailed = true;
            table.AddRow(label, check.Name ?? "", result.Display);
        }

        // Known expected manifest file.
        var manifestExists = File.Exists(Path.Join(root, ".devstart.json"));
        if (!manifestExists) anyFailed = true;
        table.AddRow("project", ".devstart.json", manifestExists ? "[green]ok[/]" : "[red]missing[/]");

        // Per-capability doctor checks + collect missing env keys for --fix.
        var missingEnv = new List<Capability.EnvAddition>();
        foreach (var capName in manifest.Capabilities)
        {
            var cap = Capability.LoadEmbedded(capName);
            foreach (var check in cap.Doctor)
            {
                var result = await CheckRunner.RunAsync(check, root);
                if (result.Outcome == CheckOutcome.Failed) anyFailed = true;
                table.AddRow(capName, $"{check.Check} {check.Name ?? check.Path ?? ""}", result.Display);

                if (check.Check == "env" && check.Name is string envKey
                    && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envKey)))
                {
                    var hint = cap.EnvAdditions.FirstOrDefault(e =>
                        string.Equals(e.Key, envKey, StringComparison.Ordinal));
                    if (hint is not null) missingEnv.Add(hint);
                }
            }
        }

        // Policy validators run informationally (doctor never fails on policy).
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

        if (anyFailed)
        {
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine(
                "[yellow]Some checks failed.[/] Run [cyan]devstart install[/] to install missing prerequisites, " +
                "or [cyan]devstart install --dry-run[/] to preview.");
            return 1;
        }

        return 0;
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
            if (existing.Contains($"{env.Key}=", StringComparison.Ordinal)) continue;
            writer.WriteLine($"{env.Key}={env.Example}");
            wrote++;
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine(
            $"[green]doctor --fix[/] wrote [cyan]{wrote}[/] entries to [grey]{Path.GetRelativePath(projectRoot, envFile)}[/]. " +
            "Review before running [cyan]just up[/].");
    }
}
