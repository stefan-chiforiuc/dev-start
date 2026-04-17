using System.Diagnostics;
using System.Reflection;
using Spectre.Console;

namespace DevStart;

/// <summary>
/// Executes a <c>dev-start new</c> scaffold: resolves capability
/// dependencies, copies embedded files (with token substitution) into the
/// target directory, applies injectors, writes the manifest, and runs
/// post-install hooks.
/// </summary>
public sealed class Planner
{
    public string RawName { get; }
    public Tokens Tokens { get; }
    public bool MultiService { get; }
    public IReadOnlyList<string> Capabilities { get; }
    public string DeployTarget { get; }
    public bool IncludeClaude { get; }

    /// <summary>File contents known to be textual — get token substitution.</summary>
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".json", ".jsonc", ".yaml", ".yml", ".md", ".http",
        ".props", ".targets", ".sln", ".editorconfig", ".gitignore", ".gitkeep",
        ".sh", ".ps1", ".cmd", ".dockerfile", ".Dockerfile", ".env", ".example",
        ".xml", ".html", ".css", ".js", ".ts", ""
    };

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
        var target = Path.GetFullPath(Tokens.KebabName);
        Directory.CreateDirectory(target);

        AnsiConsole.MarkupLine($"[grey]target:[/] {target}");

        foreach (var cap in Capabilities)
        {
            AnsiConsole.MarkupLine($"[cyan]· capability[/] {cap}");
            CopyCapabilityFiles(cap, target);
            ApplyInjectors(cap, target);
        }

        if (IncludeClaude) CopyPlatformBundle("platform/claude/", Path.Combine(target, ".claude"));
        CopyPlatformBundle("platform/compose/", target);
        CopyPlatformBundle("platform/devcontainer/", Path.Combine(target, ".devcontainer"));

        WriteManifest(target);
        TryGitInit(target);

        AnsiConsole.MarkupLine("[green]Done.[/]");
        AnsiConsole.MarkupLine("Next:");
        AnsiConsole.MarkupLine($"  cd {Tokens.KebabName}");
        AnsiConsole.MarkupLine("  just bootstrap");

        await Task.CompletedTask;
    }

    private void CopyCapabilityFiles(string capability, string targetRoot)
    {
        foreach (var rel in Capability.FilesFor(capability))
        {
            var bytes = Capability.ReadFile(capability, rel)
                ?? throw new InvalidOperationException($"Missing resource for {capability}/{rel}");

            var resolvedRel = Tokens.Apply(rel);
            var dest = Path.Combine(targetRoot, resolvedRel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            if (IsText(rel))
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                File.WriteAllText(dest, Tokens.Apply(text));
            }
            else
            {
                File.WriteAllBytes(dest, bytes);
            }
        }
    }

    private void ApplyInjectors(string capability, string targetRoot)
    {
        var injectors = Capability.LoadInjectors(capability);
        foreach (var inj in injectors.Injectors)
        {
            var file = Path.Combine(targetRoot, Tokens.Apply(inj.File));
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine($"  [yellow]skip[/] injector — target missing: [grey]{inj.File}[/]");
                continue;
            }

            var fragment = Capability.ReadFragment(capability, inj.Fragment)
                ?? throw new InvalidOperationException($"Missing fragment {capability}/{inj.Fragment}");
            fragment = Tokens.Apply(fragment);

            var content = File.ReadAllText(file);

            if (inj.Marker is { Length: > 0 })
            {
                if (!content.Contains(inj.Marker, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Marker '{inj.Marker}' not found in {inj.File}");
                }
                var rep = inj.Placement == "before"
                    ? fragment + inj.Marker
                    : inj.Placement == "replace"
                        ? fragment
                        : inj.Marker + Environment.NewLine + fragment.TrimEnd() + Environment.NewLine;
                // "after" is the default; trim trailing and re-add a single newline so repeated applies stay stable.
                content = content.Replace(inj.Marker, rep, StringComparison.Ordinal);
            }
            else if (inj.Anchor is { Length: > 0 })
            {
                if (!content.Contains(inj.Anchor, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Anchor '{inj.Anchor}' not found in {inj.File}");
                }
                var rep = inj.Placement switch
                {
                    "before" => fragment + inj.Anchor,
                    "replace" => fragment,
                    _ => inj.Anchor + Environment.NewLine + fragment
                };
                content = content.Replace(inj.Anchor, rep, StringComparison.Ordinal);
            }
            else
            {
                throw new InvalidOperationException($"Injector for {inj.File} has neither marker nor anchor.");
            }

            File.WriteAllText(file, content);
        }
    }

    private void CopyPlatformBundle(string resourcePrefix, string destRoot)
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

            var dest = Path.Combine(destRoot, Tokens.Apply(rel));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            if (IsText(rel))
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                File.WriteAllText(dest, Tokens.Apply(text));
            }
            else
            {
                File.WriteAllBytes(dest, bytes);
            }
        }
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
        if (TextExtensions.Contains(ext)) return true;
        var name = Path.GetFileName(path);
        return name.StartsWith('.') || string.Equals(name, "Dockerfile", StringComparison.Ordinal)
            || string.Equals(name, "justfile", StringComparison.Ordinal)
            || string.Equals(name, "Tiltfile", StringComparison.Ordinal);
    }
}
