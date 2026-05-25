namespace DevStart.Install;

public enum ActionCategory { Runtime, Tool, DotnetTool, Service, Manual }

public sealed record InstallAction(
    ActionCategory Category,
    string Name,
    string? Command,
    string? ManualUrl,
    bool Skipped,
    string Reason,
    Capability.DoctorCheck? SourceCheck = null);

public static class InstallPlan
{
    private static readonly HashSet<string> Runtimes =
        new(StringComparer.OrdinalIgnoreCase) { "dotnet", "node", "pnpm" };

    // Translates failing checks into install actions, using the OS-specific
    // package manager + the tool catalog. Already-satisfied checks should be
    // filtered out by the caller before this is invoked.
    public static List<InstallAction> Build(
        IEnumerable<CheckResult> failingChecks,
        PackageManager pkgManager,
        bool includeOptional)
    {
        var actions = new List<InstallAction>();

        foreach (var check in failingChecks.Select(f => f.Source))
        {
            // Required defaults to true; opt-out per capability.yaml.
            if (!check.Required && !includeOptional)
            {
                actions.Add(new InstallAction(
                    Category: ResolveCategory(check),
                    Name: check.Name ?? check.Path ?? check.Check,
                    Command: null,
                    ManualUrl: null,
                    Skipped: true,
                    Reason: "optional; pass --include-optional to install",
                    SourceCheck: check));
                continue;
            }

            switch (check.Check)
            {
                case "service":
                    actions.Add(new InstallAction(
                        Category: ActionCategory.Service,
                        Name: check.Name ?? "service",
                        Command: null,
                        ManualUrl: null,
                        Skipped: false,
                        Reason: "docker compose up",
                        SourceCheck: check));
                    break;

                case "dotnet-tool":
                    actions.Add(new InstallAction(
                        Category: ActionCategory.DotnetTool,
                        Name: check.Name ?? "",
                        Command: $"dotnet tool install -g {check.Name}",
                        ManualUrl: null,
                        Skipped: false,
                        Reason: "dotnet global tool",
                        SourceCheck: check));
                    break;

                case "tool":
                case "dotnet-version":
                    AddToolAction(actions, check, pkgManager,
                        defaultLogical: check.Check == "dotnet-version" ? "dotnet" : null);
                    break;

                case "env":
                case "file":
                    actions.Add(new InstallAction(
                        Category: ActionCategory.Manual,
                        Name: check.Name ?? check.Path ?? "",
                        Command: null,
                        ManualUrl: null,
                        Skipped: true,
                        Reason: check.Check == "env"
                            ? "env var; run `devstart doctor --fix` to seed .env.local"
                            : "missing project file; re-run scaffolding",
                        SourceCheck: check));
                    break;

                default:
                    actions.Add(new InstallAction(
                        Category: ActionCategory.Manual,
                        Name: check.Name ?? check.Check,
                        Command: null,
                        ManualUrl: null,
                        Skipped: true,
                        Reason: "no installer mapping",
                        SourceCheck: check));
                    break;
            }
        }

        return Order(actions);
    }

    private static void AddToolAction(
        List<InstallAction> actions,
        Capability.DoctorCheck check,
        PackageManager pkgManager,
        string? defaultLogical)
    {
        var logical = (check.Name ?? defaultLogical ?? "").ToLowerInvariant();
        var entry = ToolCatalog.Lookup(logical);
        var category = Runtimes.Contains(logical) ? ActionCategory.Runtime : ActionCategory.Tool;

        if (entry is null)
        {
            actions.Add(new InstallAction(
                Category: category,
                Name: logical,
                Command: null,
                ManualUrl: null,
                Skipped: true,
                Reason: "not in tool catalog",
                SourceCheck: check));
            return;
        }

        var cmd = pkgManager.BuildInstallCommand(entry);
        if (cmd is null)
        {
            actions.Add(new InstallAction(
                Category: category,
                Name: logical,
                Command: null,
                ManualUrl: entry.ManualUrl,
                Skipped: true,
                Reason: $"no {pkgManager.Name} package mapped",
                SourceCheck: check));
            return;
        }

        actions.Add(new InstallAction(
            Category: category,
            Name: logical,
            Command: cmd,
            ManualUrl: entry.ManualUrl,
            Skipped: false,
            Reason: $"via {pkgManager.Name}",
            SourceCheck: check));
    }

    private static ActionCategory ResolveCategory(Capability.DoctorCheck check)
    {
        return check.Check switch
        {
            "service" => ActionCategory.Service,
            "dotnet-tool" => ActionCategory.DotnetTool,
            "tool" or "dotnet-version" =>
                Runtimes.Contains((check.Name ?? "").ToLowerInvariant())
                    ? ActionCategory.Runtime
                    : ActionCategory.Tool,
            _ => ActionCategory.Manual,
        };
    }

    private static List<InstallAction> Order(List<InstallAction> actions)
    {
        // Runtimes first (need them on PATH for later actions), then host tools,
        // then services (docker required), then dotnet global tools (need
        // dotnet on PATH), then manual items.
        static int Rank(ActionCategory c) => c switch
        {
            ActionCategory.Runtime => 0,
            ActionCategory.Tool => 1,
            ActionCategory.Service => 2,
            ActionCategory.DotnetTool => 3,
            ActionCategory.Manual => 4,
            _ => 5,
        };
        return actions.OrderBy(a => Rank(a.Category)).ThenBy(a => a.Name, StringComparer.Ordinal).ToList();
    }
}
