using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

public static class AddCommand
{
    public static Command Build()
    {
        var capArg = new Argument<string>("capability", "Capability to add (e.g. cache, queue, s3).");
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");

        var cmd = new Command("add", "Add a capability to an existing project.") { capArg, projectOpt };

        cmd.SetHandler(async (capName, projectPath) =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);

            if (manifest.Capabilities.Contains(capName, StringComparer.Ordinal))
            {
                AnsiConsole.MarkupLine($"[yellow]{capName}[/] is already installed. Use [cyan]dev-start upgrade[/] to refresh.");
                return;
            }

            var cap = Capability.LoadEmbedded(capName);

            foreach (var dep in cap.DependsOn)
            {
                if (!manifest.Capabilities.Contains(dep, StringComparer.Ordinal))
                {
                    AnsiConsole.MarkupLine($"[red]Missing dependency[/]: [cyan]{capName}[/] requires [cyan]{dep}[/]. Install it first.");
                    return;
                }
            }

            AnsiConsole.MarkupLine($"[bold]dev-start add[/] [cyan]{capName}[/] [grey]→[/] {root}");
            AnsiConsole.MarkupLine($"[grey]{cap.Description}[/]");

            await Task.CompletedTask; // TODO: wire Planner.AddCapabilityAsync in v0.2

            manifest.Capabilities.Add(capName);
            manifest.Save(root);

            AnsiConsole.MarkupLine($"[green]Manifest updated.[/] File copy + patches are stubbed — see issue #TODO.");
        }, capArg, projectOpt);

        return cmd;
    }
}
