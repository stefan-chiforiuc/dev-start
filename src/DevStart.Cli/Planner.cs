using System.Diagnostics;
using System.Reflection;
using Spectre.Console;

namespace DevStart;

/// <summary>
/// Executes a <c>dev-start new</c> scaffold: resolves capability
/// dependencies, copies embedded files (with token substitution) into the
/// target directory, applies injectors, copies the shared platform
/// bundles, writes the manifest, and runs <c>git init</c>.
/// </summary>
public sealed class Planner
{
    public string RawName { get; }
    public Tokens Tokens { get; }
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
        RawName = name;
        Tokens = new Tokens(name);
        MultiService = multiService;
        DeployTarget = deployTarget;
        IncludeClaude = includeClaude;

        var resolved = new List<string> { "base" };
        foreach (var c in capabilities)
        {
            if (!resolved.Contains(c, StringComparer.Ordinal)) resolved.Add(c);
        }
        if (multiService && !resolved.Contains("gateway", StringComparer.Ordinal))
        {
            resolved.Add("gateway");
        }
        var deployCap = DeployCapabilityName(deployTarget);
        if (deployCap is not null && !resolved.Contains(deployCap, StringComparer.Ordinal))
        {
            resolved.Add(deployCap);
        }
        Capabilities = resolved;
    }

    private static string? DeployCapabilityName(string target) => target?.ToLowerInvariant() switch
    {
        "fly" or "flyio" or "fly.io" => "deploy-fly",
        "aca" or "azure" or "azurecontainerapps" => "deploy-aca",
        _ => null,
    };

    public Task RunAsync()
    {
        var target = Path.GetFullPath(Tokens.KebabName);
        Directory.CreateDirectory(target);

        AnsiConsole.MarkupLine($"[grey]target:[/] {target}");

        var baselines = Render(target, verbose: true);

        WriteManifest(target);
        baselines.Save(target);
        TryGitInit(target);

        AnsiConsole.MarkupLine("[green]Done.[/]");
        AnsiConsole.MarkupLine("Next:");
        AnsiConsole.MarkupLine($"  cd {Tokens.KebabName}");
        AnsiConsole.MarkupLine("  just bootstrap");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Render the template into <paramref name="target"/>: capability files +
    /// injectors + platform bundles + CLAUDE.md. Callers that want manifest
    /// + git init should use <see cref="RunAsync"/>. <c>dev-start upgrade</c>
    /// calls this directly against a staging directory.
    /// </summary>
    public Baselines Render(string target, bool verbose = false)
    {
        Directory.CreateDirectory(target);
        var baselines = new Baselines();

        foreach (var cap in Capabilities)
        {
            if (verbose) AnsiConsole.MarkupLine($"[cyan]· capability[/] {cap}");
            CapabilityInstaller.Install(cap, target, Tokens, baselines);
        }

        if (IncludeClaude)
        {
            CopyPlatformBundle("platform/claude/", Path.Join(target, ".claude"), target, baselines);
            RenderClaudeBriefing(target, baselines);
        }
        CopyPlatformBundle("platform/compose/", target, target, baselines);
        CopyPlatformBundle("platform/devcontainer/", Path.Join(target, ".devcontainer"), target, baselines);

        return baselines;
    }

    private void CopyPlatformBundle(string resourcePrefix, string destRoot, string projectRoot, Baselines? baselines)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resources = asm.GetManifestResourceNames()
            .Where(n => n.StartsWith(resourcePrefix, StringComparison.Ordinal));

        foreach (var name in resources)
        {
            var rel = name[resourcePrefix.Length..];
            if (string.IsNullOrEmpty(rel)) continue;

            using var stream = asm.GetManifestResourceStream(name)!;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();

            var appliedRel = Tokens.Apply(rel);
            var dest = Path.Join(destRoot, appliedRel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            byte[] content;
            if (IsText(rel))
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                content = System.Text.Encoding.UTF8.GetBytes(Tokens.Apply(text));
            }
            else
            {
                content = bytes;
            }
            File.WriteAllBytes(dest, content);

            // Record with a path relative to the project root, not destRoot —
            // upgrade --apply needs the same key shape as CapabilityInstaller.
            var relativeFromProject = Path.GetRelativePath(projectRoot, dest);
            baselines?.Record(relativeFromProject, content);
        }
    }

    private void RenderClaudeBriefing(string target, Baselines? baselines)
    {
        var templatePath = Path.Join(target, ".claude", "CLAUDE.md.template");
        if (!File.Exists(templatePath)) return;

        var content = File.ReadAllText(templatePath);

        var caps = string.Join(Environment.NewLine, Capabilities.Select(c =>
        {
            try
            {
                var info = Capability.LoadEmbedded(c);
                return $"- **{info.Name}** — {info.Description}";
            }
            catch
            {
                return $"- **{c}**";
            }
        }));

        var adrs = string.Join(Environment.NewLine,
        [
            "- ADR 0001 — Record architecture decisions",
            "- ADR 0002 — Minimal APIs (not controllers)",
            "- ADR 0003 — EF Core + Npgsql",
            "- ADR 0004 — Serilog + OpenTelemetry",
            "- ADR 0005 — CQRS with MediatR + outbox",
            "- ADR 0006 — Capabilities, not monolithic templates",
            "- ADR 0007 — Injectors, not per-capability template forks",
        ]);

        var extras = new List<string>();
        if (Capabilities.Contains("queue", StringComparer.Ordinal))
            extras.Add("- RabbitMQ: `localhost:5672` (management UI at :15672)");
        if (Capabilities.Contains("cache", StringComparer.Ordinal))
            extras.Add("- Redis: `localhost:6379`");

        content = content
            .Replace("{{ProjectName}}", Tokens.Name, StringComparison.Ordinal)
            .Replace("{{CapabilitiesList}}", caps, StringComparison.Ordinal)
            .Replace("{{AdrList}}", adrs, StringComparison.Ordinal)
            .Replace("{{ConditionalServices}}", string.Join(Environment.NewLine, extras), StringComparison.Ordinal);

        var claudeMdPath = Path.Join(target, ".claude", "CLAUDE.md");
        File.WriteAllText(claudeMdPath, content);
        baselines?.Record(Path.GetRelativePath(target, claudeMdPath), content);
        // The template itself was baseline-recorded during CopyPlatformBundle;
        // forget it now so upgrade --apply doesn't try to reconcile a deleted file.
        baselines?.Forget(Path.GetRelativePath(target, templatePath));
        File.Delete(templatePath);
    }

    private void WriteManifest(string target)
    {
        var manifest = new Manifest
        {
            Name = Tokens.KebabName,
            Capabilities = [.. Capabilities],
            Services = MultiService ? ["gateway", "users", "orders"] : ["api"],
            Deploy = DeployTarget,
        };
        manifest.Save(target);
    }

    private static void TryGitInit(string target)
    {
        try
        {
            Run("git", "init -b main", target);
            Run("git", "add -A", target);
            Run("git", "-c user.name=dev-start -c user.email=noreply@dev-start.dev commit -m \"chore: initial scaffold by dev-start\" --no-gpg-sign --quiet", target);
        }
        catch
        {
            AnsiConsole.MarkupLine("[yellow]git init skipped[/] — run it manually.");
        }
    }

    private static void Run(string cmd, string args, string cwd)
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException($"{cmd} {args} failed: {p.StandardError.ReadToEnd()}");
        }
    }

    private static bool IsText(string path)
    {
        var ext = Path.GetExtension(path);
        return ext switch
        {
            ".cs" or ".csproj" or ".json" or ".jsonc" or ".yaml" or ".yml" or ".md"
            or ".http" or ".props" or ".targets" or ".sln" or ".editorconfig"
            or ".gitignore" or ".gitkeep" or ".sh" or ".ps1" or ".cmd"
            or ".dockerfile" or ".env" or ".example" or ".xml" or ".html"
            or ".css" or ".js" or ".ts" or ".toml" or ".bicep" or ".tf"
            or "" => true,
            _ => Path.GetFileName(path) is "Dockerfile" or "justfile" or "Tiltfile",
        };
    }
}
