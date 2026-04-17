using System.CommandLine;
using System.Net.Sockets;
using Spectre.Console;

namespace DevStart.Commands;

public static class DoctorCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".", "Path to the target project.");
        var cmd = new Command("doctor", "Diagnose a project for drift, missing env vars, and broken services.") { projectOpt };

        cmd.SetHandler(async (projectPath) =>
        {
            var root = Path.GetFullPath(projectPath);
            var manifest = Manifest.Load(root);

            AnsiConsole.MarkupLine($"[bold]dev-start doctor[/] [grey]→[/] {root}");
            AnsiConsole.MarkupLine($"manifest: [cyan]{manifest.Name}[/] v[cyan]{manifest.TemplateVersion}[/]");
            AnsiConsole.MarkupLine($"capabilities: {string.Join(", ", manifest.Capabilities)}");
            AnsiConsole.WriteLine();

            var table = new Table().AddColumns("Check", "Result");

            foreach (var capName in manifest.Capabilities)
            {
                var cap = Capability.LoadEmbedded(capName);
                foreach (var check in cap.Doctor)
                {
                    var result = await RunCheckAsync(check, root);
                    table.AddRow($"{capName}:{check.Check} {check.Name ?? check.Path ?? ""}", result);
                }
            }

            AnsiConsole.Write(table);
        }, projectOpt);

        return cmd;
    }

    private static async Task<string> RunCheckAsync(Capability.DoctorCheck check, string projectRoot)
    {
        try
        {
            return check.Check switch
            {
                "service" when check.Port is int port => await CheckPortAsync("localhost", port),
                "env" when check.Name is string key => CheckEnv(key),
                "file" when check.Path is string rel => File.Exists(Path.Combine(projectRoot, rel))
                    ? "[green]ok[/]"
                    : "[red]missing[/]",
                "dotnet-version" => "[grey]TODO[/]",
                "dotnet-tool" => "[grey]TODO[/]",
                _ => "[grey]unknown check[/]",
            };
        }
        catch (Exception ex)
        {
            return $"[red]error[/] {ex.Message}";
        }
    }

    private static async Task<string> CheckPortAsync(string host, int port)
    {
        using var client = new TcpClient();
        var task = client.ConnectAsync(host, port);
        var completed = await Task.WhenAny(task, Task.Delay(500));
        return completed == task && client.Connected ? "[green]listening[/]" : "[red]unreachable[/]";
    }

    private static string CheckEnv(string key)
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)) ? "[green]set[/]" : "[red]missing[/]";
}
