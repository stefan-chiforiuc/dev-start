using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

public static class NewCommand
{
    public static Command Build()
    {
        var nameArg = new Argument<string>("name", "Project name (kebab-case).");
        var multiOpt = new Option<bool>("--multi-service", "Scaffold a multi-service layout with a gateway.");
        var capsOpt = new Option<string[]>("--with", "Capabilities to include (space-separated).") { AllowMultipleArgumentsPerToken = true };
        var deployOpt = new Option<string>("--deploy", () => "none", "Deploy target: none | fly | aca.");
        var noClaudeOpt = new Option<bool>("--no-claude", "Skip the .claude/ AI bundle.");
        var stackOpt = new Option<string>("--stack", () => "dotnet",
            "Stack: dotnet (default) | typescript.");

        var cmd = new Command("new", "Scaffold a new dev-start project.")
        {
            nameArg, multiOpt, capsOpt, deployOpt, noClaudeOpt, stackOpt,
        };

        cmd.SetHandler(async (name, multi, caps, deploy, noClaude, stackRaw) =>
        {
            var stack = Planner.NormalizeStack(stackRaw);

            var defaults = stack == Planner.StackTypescript
                ? new[] { "ts-postgres", "ts-auth", "ts-otel" }
                : new[] { "postgres", "auth", "otel" };

            var planner = new Planner(
                name: name,
                multiService: multi,
                capabilities: caps.Length > 0 ? caps : defaults,
                deployTarget: deploy,
                includeClaude: !noClaude,
                stack: stack);

            AnsiConsole.MarkupLine($"[bold]dev-start new[/] [grey]—[/] [cyan]{name}[/]");
            AnsiConsole.MarkupLine($"stack:         [yellow]{stack}[/]");
            AnsiConsole.MarkupLine($"architecture:  [yellow]{(multi ? "multi-service" : "monolith")}[/]");
            AnsiConsole.MarkupLine($"capabilities:  [yellow]{string.Join(", ", planner.Capabilities)}[/]");
            AnsiConsole.MarkupLine($"deploy:        [yellow]{deploy}[/]");
            AnsiConsole.MarkupLine($"claude bundle: [yellow]{(noClaude ? "no" : "yes")}[/]");
            AnsiConsole.WriteLine();

            await planner.RunAsync();

            AnsiConsole.MarkupLine($"[green]Done.[/] Next:");
            AnsiConsole.MarkupLine($"  cd {name}");
            AnsiConsole.MarkupLine($"  just bootstrap");
        }, nameArg, multiOpt, capsOpt, deployOpt, noClaudeOpt, stackOpt);

        return cmd;
    }
}
