namespace DevStart.Install;

public sealed record ToolEntry(
    string Logical,
    string? BrewPkg,
    string? AptPkg,
    string? DnfPkg,
    string? WingetId,
    string ManualUrl);

public static class ToolCatalog
{
    private static readonly Dictionary<string, ToolEntry> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dotnet"] = new("dotnet",
            BrewPkg: "dotnet-sdk",
            AptPkg: "dotnet-sdk-9.0",
            DnfPkg: "dotnet-sdk-9.0",
            WingetId: "Microsoft.DotNet.SDK.9",
            ManualUrl: "https://dotnet.microsoft.com/download"),

        ["node"] = new("node",
            BrewPkg: "node",
            AptPkg: "nodejs",
            DnfPkg: "nodejs",
            WingetId: "OpenJS.NodeJS.LTS",
            ManualUrl: "https://nodejs.org/"),

        ["pnpm"] = new("pnpm",
            BrewPkg: "pnpm",
            AptPkg: null,
            DnfPkg: null,
            WingetId: "pnpm.pnpm",
            ManualUrl: "https://pnpm.io/installation"),

        ["just"] = new("just",
            BrewPkg: "just",
            AptPkg: "just",
            DnfPkg: "just",
            WingetId: "Casey.Just",
            ManualUrl: "https://github.com/casey/just#installation"),

        ["bash"] = new("bash",
            BrewPkg: "bash",
            AptPkg: "bash",
            DnfPkg: "bash",
            WingetId: "Git.Git",
            ManualUrl: "https://gitforwindows.org/"),

        ["docker"] = new("docker",
            BrewPkg: "docker",
            AptPkg: "docker.io",
            DnfPkg: "docker",
            WingetId: "Docker.DockerDesktop",
            ManualUrl: "https://docs.docker.com/get-docker/"),

        ["flyctl"] = new("flyctl",
            BrewPkg: "flyctl",
            AptPkg: null,
            DnfPkg: null,
            WingetId: "Fly-io.flyctl",
            ManualUrl: "https://fly.io/docs/flyctl/install/"),

        ["az"] = new("az",
            BrewPkg: "azure-cli",
            AptPkg: "azure-cli",
            DnfPkg: "azure-cli",
            WingetId: "Microsoft.AzureCLI",
            ManualUrl: "https://learn.microsoft.com/cli/azure/install-azure-cli"),

        ["git"] = new("git",
            BrewPkg: "git",
            AptPkg: "git",
            DnfPkg: "git",
            WingetId: "Git.Git",
            ManualUrl: "https://git-scm.com/downloads"),
    };

    public static ToolEntry? Lookup(string logicalName)
        => Entries.TryGetValue(logicalName, out var e) ? e : null;

    public static IReadOnlyCollection<string> KnownNames() => Entries.Keys;
}
