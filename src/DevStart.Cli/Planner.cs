using Spectre.Console;

namespace DevStart;

/// <summary>
/// Plans and executes a <c>dev-start new</c> scaffold: resolves capability
/// dependencies, copies embedded files into the target directory, applies
/// patches, writes the manifest, and runs post-install hooks.
/// </summary>
public sealed class Planner
{
    public string Name { get; }
    public bool MultiService { get; }
    public IReadOnlyList<string> Capabilities { get; }
    public string DeployTarget { get; }
    public bool IncludeClaude { get; }

    public Planner(
        string name,
        bool multiService,
        IEnumerable<string> capabilities,
        string deployTarget,
        bool includeClaude)
    {
        Name = name;
        MultiService = multiService;
        DeployTarget = deployTarget;
        IncludeClaude = includeClaude;

        var resolved = new List<string> { "base" };
        foreach (var c in capabilities)
        {
            if (!resolved.Contains(c, StringComparer.Ordinal))
            {
                resolved.Add(c);
            }
        }
        if (multiService && !resolved.Contains("gateway", StringComparer.Ordinal))
        {
            resolved.Add("gateway");
        }
        Capabilities = resolved;
    }

    public async Task RunAsync()
    {
        var target = Path.GetFullPath(Name);
        Directory.CreateDirectory(target);

        AnsiConsole.MarkupLine($"[grey]target:[/] {target}");

        // TODO v0.2:
        //   1. For each capability in dependency order:
        //      a. Enumerate embedded resources under `capabilities/<name>/files/**`.
        //      b. For each resource, replace `{{Name}}` tokens with PascalCase project name,
        //         `{{name}}` with kebab-case, etc.
        //      c. Write to target respecting directory tree.
        //      d. Apply any patches under `capabilities/<name>/patches/**`.
        //   2. Copy the platform/claude bundle into <target>/.claude/ if IncludeClaude.
        //   3. Copy platform/compose/* into <target>/.
        //   4. Copy platform/devcontainer into <target>/.devcontainer/.
        //   5. Write .devstart.json.
        //   6. git init + git add + single initial commit.
        //   7. Run each capability's postInstall steps (dotnet restore, ef update, etc.).

        var manifest = new Manifest
        {
            Name = Name,
            Capabilities = [.. Capabilities],
            Services = MultiService ? ["gateway", "users", "orders"] : ["api"],
            Deploy = DeployTarget,
        };
        manifest.Save(target);
        AnsiConsole.MarkupLine("[green]wrote[/] .devstart.json");

        AnsiConsole.MarkupLine("[yellow]Scaffolding is stubbed in v0.1.[/]");
        AnsiConsole.MarkupLine("[grey]Tracking: issue #TODO — file copy, token replacement, patch application, git init.[/]");

        await Task.CompletedTask;
    }
}
