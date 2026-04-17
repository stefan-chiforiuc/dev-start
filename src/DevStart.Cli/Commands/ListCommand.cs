using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

public static class ListCommand
{
    public static Command Build()
    {
        var cmd = new Command("list", "List available capabilities.");

        cmd.SetHandler(() =>
        {
            var table = new Table().AddColumns("name", "version", "description");
            foreach (var name in Capability.AvailableNames())
            {
                try
                {
                    var c = Capability.LoadEmbedded(name);
                    table.AddRow(c.Name, c.Version, c.Description);
                }
                catch (Exception ex)
                {
                    table.AddRow(name, "-", $"[red]{ex.Message}[/]");
                }
            }
            AnsiConsole.Write(table);
        });

        return cmd;
    }
}
