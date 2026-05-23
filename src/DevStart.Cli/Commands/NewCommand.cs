using System.CommandLine;
using DevStart.Wizard;
using Spectre.Console;

namespace DevStart.Commands;

public static class NewCommand
{
    public static Command Build()
    {
        var nameArg = new Argument<string>("name", "Project name (kebab-case).");
        var multiOpt = new Option<bool?>("--multi-service", "Scaffold a multi-service layout with a gateway.");
        var capsOpt = new Option<string[]>("--with", "Capabilities to include (space-separated).") { AllowMultipleArgumentsPerToken = true };
        var deployOpt = new Option<string?>("--deploy", "Deploy target: none | fly | aca.");
        var noClaudeOpt = new Option<bool>("--no-claude", "Skip the .claude/ AI bundle.");
        var stackOpt = new Option<string?>("--stack", "Stack: dotnet (default) | typescript.");
        var frameworkOpt = new Option<string?>("--framework", "Backend framework variant (e.g. aspnet, fastify).");
        var versionOpt = new Option<string?>("--framework-version", "Backend framework version (e.g. 8, 9, 5).");
        var noInteractiveOpt = new Option<bool>("--no-interactive", "Skip the wizard; use flag values + defaults.");

        var cmd = new Command("new", "Scaffold a new dev-start project.")
        {
            nameArg, multiOpt, capsOpt, deployOpt, noClaudeOpt, stackOpt,
            frameworkOpt, versionOpt, noInteractiveOpt,
        };

        cmd.SetHandler(async ctx =>
        {
            var name = ctx.ParseResult.GetValueForArgument(nameArg);
            var multi = ctx.ParseResult.GetValueForOption(multiOpt);
            var caps = ctx.ParseResult.GetValueForOption(capsOpt) ?? Array.Empty<string>();
            var deploy = ctx.ParseResult.GetValueForOption(deployOpt);
            var noClaude = ctx.ParseResult.GetValueForOption(noClaudeOpt);
            var stackRaw = ctx.ParseResult.GetValueForOption(stackOpt);
            var framework = ctx.ParseResult.GetValueForOption(frameworkOpt);
            var version = ctx.ParseResult.GetValueForOption(versionOpt);
            var noInteractive = ctx.ParseResult.GetValueForOption(noInteractiveOpt);

            var interactive = !noInteractive && !Console.IsInputRedirected;

            // Build the preset: any flag the user passed becomes a pre-answer.
            // `--with` and `--no-claude` are special — their presence is the
            // signal. For `--with` we treat any value as "you've already
            // chosen extras."
            var preset = new NewWizard.Preset(
                Stack: stackRaw,
                Framework: framework,
                FrameworkVersion: version,
                Extras: caps.Length > 0 ? caps : null,
                DeployTarget: deploy,
                IncludeClaude: noClaude ? false : (bool?)null,
                MultiService: multi);

            var answers = new NewWizard().Run(preset, interactive);

            var planner = new Planner(
                name: name,
                multiService: answers.MultiService,
                capabilities: answers.Extras,
                deployTarget: answers.DeployTarget,
                includeClaude: answers.IncludeClaude,
                stack: answers.Stack,
                backendFramework: string.IsNullOrEmpty(answers.Framework) ? null : answers.Framework,
                backendVersion: string.IsNullOrEmpty(answers.FrameworkVersion) ? null : answers.FrameworkVersion);

            AnsiConsole.MarkupLine($"[bold]dev-start new[/] [grey]—[/] [cyan]{name}[/]");
            AnsiConsole.MarkupLine($"stack:         [yellow]{answers.Stack}[/]");
            AnsiConsole.MarkupLine($"backend:       [yellow]{FormatBackend(answers.Framework, answers.FrameworkVersion)}[/]");
            AnsiConsole.MarkupLine($"architecture:  [yellow]{(answers.MultiService ? "multi-service" : "monolith")}[/]");
            AnsiConsole.MarkupLine($"capabilities:  [yellow]{string.Join(", ", planner.Capabilities)}[/]");
            AnsiConsole.MarkupLine($"deploy:        [yellow]{answers.DeployTarget}[/]");
            AnsiConsole.MarkupLine($"claude bundle: [yellow]{(answers.IncludeClaude ? "yes" : "no")}[/]");
            AnsiConsole.WriteLine();

            await planner.RunAsync();

            AnsiConsole.MarkupLine($"[green]Done.[/] Next:");
            AnsiConsole.MarkupLine($"  cd {name}");
            AnsiConsole.MarkupLine($"  just bootstrap");
        });

        return cmd;
    }

    private static string FormatBackend(string framework, string version)
    {
        if (string.IsNullOrEmpty(framework)) return "(default)";
        return string.IsNullOrEmpty(version) ? framework : $"{framework} {version}";
    }
}
