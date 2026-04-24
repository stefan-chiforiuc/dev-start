using System.Diagnostics;
using Spectre.Console;

namespace DevStart;

/// <summary>
/// Installs a capability into a target project: copies embedded files
/// (with token substitution), then applies the capability's injectors.
/// Shared between <c>dev-start new</c> (via <see cref="Planner"/>) and
/// <c>dev-start add</c>.
/// </summary>
public static class CapabilityInstaller
{
    /// <summary>File extensions that get token substitution in their contents.</summary>
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".json", ".jsonc", ".yaml", ".yml", ".md", ".http",
        ".props", ".targets", ".sln", ".editorconfig", ".gitignore", ".gitkeep",
        ".sh", ".ps1", ".cmd", ".dockerfile", ".env", ".example", ".fragment",
        ".xml", ".html", ".css", ".js", ".ts", ".tsx", ".jsx", ".mjs", ".cjs",
        ".toml", ".bicep", ".tf", ".sql", ".graphql", ".svg", "",
    };

    public static void Install(
        string capability, string targetRoot, Tokens tokens, Baselines? baselines = null)
    {
        CopyFiles(capability, targetRoot, tokens, baselines);
        ApplyInjectors(capability, targetRoot, tokens, baselines);
    }

    public static void CopyFiles(
        string capability, string targetRoot, Tokens tokens, Baselines? baselines = null)
    {
        foreach (var rel in Capability.FilesFor(capability))
        {
            var bytes = Capability.ReadFile(capability, rel)
                ?? throw new InvalidOperationException($"Missing resource for {capability}/{rel}");

            var relativeOutPath = tokens.Apply(rel);
            var dest = Path.Join(targetRoot, relativeOutPath);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            byte[] content;
            if (IsText(rel))
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                content = System.Text.Encoding.UTF8.GetBytes(tokens.Apply(text));
            }
            else
            {
                content = bytes;
            }

            if (File.Exists(dest))
            {
                var existing = File.ReadAllBytes(dest);
                if (existing.AsSpan().SequenceEqual(content))
                {
                    // Already identical — record baseline anyway so upgrade
                    // knows this file is template-tracked.
                    baselines?.Record(relativeOutPath, content);
                    continue;
                }
                AnsiConsole.MarkupLine(
                    $"  [yellow]skip[/] {rel.EscapeMarkup()} — already exists and differs; remove it first to re-install");
                continue;
            }

            File.WriteAllBytes(dest, content);
            baselines?.Record(relativeOutPath, content);

            // Auto-register new .csproj files in the solution so `dotnet build`
            // picks them up without the user needing to run `dotnet sln add`.
            if (dest.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                TryRegisterInSolution(targetRoot, dest);
            }
        }
    }

    private static void TryRegisterInSolution(string projectRoot, string csprojPath)
    {
        var sln = Directory.EnumerateFiles(projectRoot, "*.sln", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
        if (sln is null) return; // multi-service layouts may not have a root sln

        try
        {
            var psi = new ProcessStartInfo("dotnet", $"sln \"{sln}\" add \"{csprojPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = projectRoot,
            };
            using var p = Process.Start(psi);
            if (p is null) return;
            p.WaitForExit(10_000);
            // `dotnet sln add` is idempotent — it prints "Project already has a
            // reference" and exits 0 on re-add. Non-zero means something genuinely
            // went wrong (SDK missing, malformed sln); don't fail the install.
            if (p.ExitCode != 0)
            {
                AnsiConsole.MarkupLine(
                    $"  [yellow]warn[/] couldn't auto-register {Path.GetFileName(csprojPath)} in the solution; run [cyan]dotnet sln add[/] manually");
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // dotnet SDK not on PATH — not a hard failure; the user can add manually.
        }
        catch (InvalidOperationException)
        {
            // Process couldn't start for another reason.
        }
    }

    public static void ApplyInjectors(
        string capability, string targetRoot, Tokens tokens, Baselines? baselines = null)
    {
        var spec = Capability.LoadInjectors(capability);
        ApplyInjectors(
            spec.Injectors,
            targetRoot,
            tokens,
            baselines,
            fragmentReader: p => Capability.ReadFragment(capability, p));
    }

    /// <summary>
    /// Apply a list of injector specs using a caller-supplied fragment reader.
    /// Policies reuse this path — they load fragments from
    /// <c>policies/&lt;name&gt;/fragments/</c> instead of
    /// <c>capabilities/&lt;name&gt;/injectors/</c>.
    /// </summary>
    public static void ApplyInjectors(
        IEnumerable<InjectorSpec> specs,
        string targetRoot,
        Tokens tokens,
        Baselines? baselines,
        Func<string, string?> fragmentReader)
    {
        foreach (var inj in specs)
        {
            var relativeFile = tokens.Apply(inj.File);
            var file = Path.Join(targetRoot, relativeFile);
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine($"  [yellow]skip[/] injector — target missing: [grey]{inj.File}[/]");
                continue;
            }

            var fragment = fragmentReader(inj.Fragment)
                ?? throw new InvalidOperationException($"Missing fragment {inj.Fragment}");
            fragment = tokens.Apply(fragment);

            var content = File.ReadAllText(file);
            content = ApplyOne(content, inj, fragment);
            File.WriteAllText(file, content);

            // Injectors mutate files base shipped. The new combined state
            // is now the template baseline for this file.
            baselines?.Record(relativeFile, content);
        }
    }

    private static string ApplyOne(string content, InjectorSpec inj, string fragment)
    {
        if (string.Equals(inj.Mode, "json-merge", StringComparison.OrdinalIgnoreCase))
        {
            return JsonMerger.Merge(content, fragment);
        }

        if (inj.Marker is { Length: > 0 })
        {
            if (!content.Contains(inj.Marker, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Marker '{inj.Marker}' not found in {inj.File}");
            }

            if (FragmentAlreadyPresent(content, fragment)) return content;

            var rep = inj.Placement switch
            {
                "before" => fragment.TrimEnd() + Environment.NewLine + inj.Marker,
                "replace" => fragment,
                _ => inj.Marker + Environment.NewLine + fragment.TrimEnd() + Environment.NewLine,
            };
            return content.Replace(inj.Marker, rep, StringComparison.Ordinal);
        }

        if (inj.Anchor is { Length: > 0 })
        {
            if (!content.Contains(inj.Anchor, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Anchor '{inj.Anchor}' not found in {inj.File}");
            }

            if (FragmentAlreadyPresent(content, fragment)) return content;

            var rep = inj.Placement switch
            {
                "before" => fragment.TrimEnd() + Environment.NewLine + inj.Anchor,
                "replace" => fragment,
                _ => inj.Anchor + Environment.NewLine + fragment,
            };
            return content.Replace(inj.Anchor, rep, StringComparison.Ordinal);
        }

        throw new InvalidOperationException($"Injector for {inj.File} has neither marker nor anchor.");
    }

    /// <summary>
    /// Cheap idempotency check: if the trimmed fragment body is already in
    /// the file, we skip. Avoids double-applying when <c>add</c> is rerun.
    /// </summary>
    private static bool FragmentAlreadyPresent(string content, string fragment)
    {
        var needle = fragment.Trim();
        return needle.Length > 0 && content.Contains(needle, StringComparison.Ordinal);
    }

    private static bool IsText(string path)
    {
        var ext = Path.GetExtension(path);
        if (TextExtensions.Contains(ext)) return true;
        var name = Path.GetFileName(path);
        return name.StartsWith('.') || name is "Dockerfile" or "justfile" or "Tiltfile"
            or "pnpm-workspace.yaml" or "pnpm-lock.yaml";
    }
}
