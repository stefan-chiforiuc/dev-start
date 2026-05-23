using Spectre.Console;

namespace DevStart.Wizard;

/// <summary>
/// Interactive question flow for <c>dev-start new</c>. Runs by default when
/// stdin is a TTY and <c>--no-interactive</c> is not set. Any flag explicitly
/// provided by the user is treated as a pre-answer for that prompt (the
/// wizard skips that question entirely).
/// </summary>
public sealed class NewWizard
{
    public sealed record Answers(
        string Stack,
        string Framework,
        string FrameworkVersion,
        IReadOnlyList<string> Extras,
        string DeployTarget,
        bool IncludeClaude,
        bool MultiService);

    public sealed record Preset(
        string? Stack = null,
        string? Framework = null,
        string? FrameworkVersion = null,
        IReadOnlyList<string>? Extras = null,
        string? DeployTarget = null,
        bool? IncludeClaude = null,
        bool? MultiService = null);

    private readonly IAnsiConsole _console;

    public NewWizard(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    /// <summary>
    /// Run the wizard with the given preset (flag-provided answers).
    /// Returns the final set of answers (preset values pass through;
    /// missing ones are asked, falling back to defaults if non-interactive).
    /// </summary>
    public Answers Run(Preset preset, bool interactive)
    {
        var stack = preset.Stack ?? (interactive ? AskStack() : Planner.StackDotnet);
        stack = Planner.NormalizeStack(stack);

        var (framework, version) = ResolveBackend(stack, preset.Framework, preset.FrameworkVersion, interactive);

        var extras = preset.Extras ?? (interactive ? AskExtras(stack) : DefaultExtras(stack));

        var deploy = preset.DeployTarget ?? (interactive ? AskDeploy() : "none");

        var includeClaude = preset.IncludeClaude ?? (interactive ? AskIncludeClaude() : true);

        // multi-service is rare; only ask in interactive mode.
        var multi = preset.MultiService ?? (interactive ? AskMultiService() : false);

        return new Answers(stack, framework, version, extras, deploy, includeClaude, multi);
    }

    private string AskStack()
    {
        return _console.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Stack[/] — which runtime?")
            .AddChoices("dotnet", "typescript")
            .UseConverter(s => s switch
            {
                "dotnet" => "dotnet  — ASP.NET Core minimal API (default)",
                "typescript" => "typescript  — Fastify + pnpm workspace",
                _ => s,
            }));
    }

    private (string framework, string version) ResolveBackend(
        string stack, string? presetFramework, string? presetVersion, bool interactive)
    {
        var variants = CapabilityResolver.ListFamily("backend", stack);
        if (variants.Count == 0)
        {
            // No `family: backend` declared yet — fall back to defaults.
            var fwDefault = stack == Planner.StackTypescript ? "fastify" : "aspnet";
            return (presetFramework ?? fwDefault, presetVersion ?? "");
        }

        var framework = presetFramework
            ?? (interactive ? AskFramework(variants) : DefaultFramework(variants));

        var byFramework = variants
            .Where(v => string.Equals(v.Framework, framework, StringComparison.OrdinalIgnoreCase))
            .ToList();

        string version;
        if (presetVersion is not null)
        {
            version = presetVersion;
        }
        else if (byFramework.Count <= 1)
        {
            version = byFramework.FirstOrDefault()?.FrameworkVersion ?? "";
        }
        else
        {
            version = interactive ? AskVersion(byFramework) : DefaultVersion(byFramework);
        }
        return (framework, version);
    }

    private string AskFramework(IReadOnlyList<Capability> variants)
    {
        var frameworks = variants
            .Select(v => v.Framework ?? "")
            .Where(f => f.Length > 0)
            .Distinct()
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        if (frameworks.Count == 1) return frameworks[0];

        return _console.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Backend framework[/]")
            .AddChoices(frameworks));
    }

    private string DefaultFramework(IReadOnlyList<Capability> variants)
    {
        return variants
            .Select(v => v.Framework ?? "")
            .Where(f => f.Length > 0)
            .OrderBy(f => f, StringComparer.Ordinal)
            .FirstOrDefault() ?? "";
    }

    private string AskVersion(IReadOnlyList<Capability> variants)
    {
        var versions = variants
            .Select(v => v.FrameworkVersion ?? "")
            .Where(v => v.Length > 0)
            .OrderByDescending(v => v, StringComparer.Ordinal)
            .ToList();
        if (versions.Count == 0) return "";

        return _console.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Framework version[/] — newest is the default")
            .AddChoices(versions));
    }

    private string DefaultVersion(IReadOnlyList<Capability> variants)
    {
        return variants
            .Select(v => v.FrameworkVersion ?? "")
            .Where(v => v.Length > 0)
            .OrderByDescending(v => v, StringComparer.Ordinal)
            .FirstOrDefault() ?? "";
    }

    private static readonly string[] ExtrasMenu =
    {
        "postgres", "auth", "otel", "cache", "queue", "s3", "mail", "flags", "sdk", "frontend",
    };

    private IReadOnlyList<string> AskExtras(string stack)
    {
        var defaults = DefaultExtras(stack);
        var prompt = new MultiSelectionPrompt<string>()
            .Title("[bold]Extras[/] — toggle with [yellow]<space>[/], confirm with [yellow]<enter>[/]")
            .NotRequired()
            .PageSize(12)
            .AddChoices(ExtrasMenu);

        foreach (var d in defaults) prompt.Select(d);
        return _console.Prompt(prompt);
    }

    private static IReadOnlyList<string> DefaultExtras(string stack)
        => new[] { "postgres", "auth", "otel" };

    private string AskDeploy()
    {
        return _console.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Deploy target[/]")
            .AddChoices("none", "fly", "aca")
            .UseConverter(s => s switch
            {
                "none" => "none  — decide later",
                "fly" => "fly   — Fly.io",
                "aca" => "aca   — Azure Container Apps",
                _ => s,
            }));
    }

    private bool AskIncludeClaude()
    {
        return _console.Prompt(new ConfirmationPrompt("Include the [cyan].claude/[/] AI bundle?")
        {
            DefaultValue = true,
        });
    }

    private bool AskMultiService()
    {
        return _console.Prompt(new ConfirmationPrompt("Scaffold a multi-service layout with a gateway?")
        {
            DefaultValue = false,
        });
    }
}
