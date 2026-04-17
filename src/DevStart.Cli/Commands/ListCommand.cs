using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

public static class ListCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".",
            "If set, annotate the list with what's installed in this project.");

        var cmd = new Command("list", "List available capabilities.") { projectOpt };

        cmd.SetHandler(projectPath =>
        {
            HashSet<string> installed = [];
            try
            {
                var manifest = Manifest.Load(Path.GetFullPath(projectPath));
                foreach (var c in manifest.Capabilities) installed.Add(c);
            }
            catch
            {
                // Not inside a dev-start project; show plain list.
            }

            var table = new Table()
                .AddColumn("status")
                .AddColumn("name")
                .AddColumn("version")
                .AddColumn("depends on")
                .AddColumn("description");

            foreach (var name in Capability.AvailableNames())
            {
                try
                {
                    var c = Capability.LoadEmbedded(name);
                    var status = installed.Contains(name) ? "[green]●[/]" : "[grey]○[/]";
                    var deps = c.DependsOn.Count > 0
                        ? string.Join(", ", c.DependsOn)
                        : "[grey]—[/]";
                    table.AddRow(status, c.Name, c.Version, deps, c.Description);
                }
                catch (Exception ex)
                {
                    table.AddRow("[red]![/]", name, "-", "-", $"[red]{ex.Message}[/]");
                }
            }

            AnsiConsole.Write(table);

            if (installed.Count > 0)
            {
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[green]●[/] installed  [grey]○[/] available");
            }
        }, projectOpt);

        return cmd;
    }
}
