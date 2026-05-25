using System.Net.Sockets;

namespace DevStart.Install;

public static class ServiceStarter
{
    // Compose-key aliases: e.g. the TS-stack capability declares `mailhog-smtp`
    // but the compose service is named `mailhog`.
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mailhog-smtp"] = "mailhog",
    };

    public static string Normalize(string name)
        => Aliases.TryGetValue(name, out var canon) ? canon : name;

    public static async Task<(bool Started, string Message)> StartAsync(
        IEnumerable<InstallAction> serviceActions,
        IProcessRunner runner,
        string projectRoot,
        CancellationToken ct = default)
    {
        var names = serviceActions
            .Where(a => a.Category == ActionCategory.Service && !a.Skipped)
            .Select(a => Normalize(a.Name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (names.Count == 0)
            return (true, "no services to start");

        var composeFile = Path.Join(projectRoot, "platform", "compose", "docker-compose.yml");
        var composeArgs = File.Exists(composeFile)
            ? $"compose -f \"{composeFile}\" up -d {string.Join(' ', names)}"
            : $"compose up -d {string.Join(' ', names)}";

        var result = await runner.RunAsync("docker", composeArgs, ct);
        if (result.ExitCode != 0)
        {
            return (false, $"docker compose failed: {result.StdErr.Trim()}");
        }

        // Best-effort: wait briefly for any service ports to become reachable
        // by re-running the original port checks.
        var ports = serviceActions
            .Select(a => a.SourceCheck)
            .Where(c => c?.Port is int)
            .Select(c => c!.Port!.Value)
            .Distinct()
            .ToList();
        foreach (var port in ports)
        {
            await WaitForPort("localhost", port, TimeSpan.FromSeconds(30), ct);
        }

        return (true, $"started {string.Join(", ", names)}");
    }

    private static async Task WaitForPort(string host, int port, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            using var client = new TcpClient();
            try
            {
                var connect = client.ConnectAsync(host, port);
                var done = await Task.WhenAny(connect, Task.Delay(500, ct));
                if (done == connect && client.Connected) return;
            }
            catch (SocketException)
            {
                // try again
            }
            await Task.Delay(500, ct);
        }
    }
}
