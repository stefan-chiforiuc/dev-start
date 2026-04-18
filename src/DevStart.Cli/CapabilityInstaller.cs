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
        ".xml", ".html", ".css", ".js", ".ts", ".toml", ".bicep", ".tf", "",
    };

    public static void Install(string capability, string targetRoot, Tokens tokens)
    {
        CopyFiles(capability, targetRoot, tokens);
        ApplyInjectors(capability, targetRoot, tokens);
    }

    public static void CopyFiles(string capability, string targetRoot, Tokens tokens)
    {
        foreach (var rel in Capability.FilesFor(capability))
        {
            var bytes = Capability.ReadFile(capability, rel)
                ?? throw new InvalidOperationException($"Missing resource for {capability}/{rel}");

            var dest = Path.Combine(targetRoot, tokens.Apply(rel));
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
                    continue; // already identical — no-op, preserves mtime
                }
                AnsiConsole.MarkupLine(
                    $"  [yellow]skip[/] {rel.EscapeMarkup()} — already exists and differs; remove it first to re-install");
                continue;
            }

            File.WriteAllBytes(dest, content);
        }
    }

    public static void ApplyInjectors(string capability, string targetRoot, Tokens tokens)
    {
        var spec = Capability.LoadInjectors(capability);
        foreach (var inj in spec.Injectors)
        {
            var file = Path.Combine(targetRoot, tokens.Apply(inj.File));
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine($"  [yellow]skip[/] injector — target missing: [grey]{inj.File}[/]");
                continue;
            }

            var fragment = Capability.ReadFragment(capability, inj.Fragment)
                ?? throw new InvalidOperationException($"Missing fragment {capability}/{inj.Fragment}");
            fragment = tokens.Apply(fragment);

            var content = File.ReadAllText(file);
            content = ApplyOne(content, inj, fragment);
            File.WriteAllText(file, content);
        }
    }

    private static string ApplyOne(string content, InjectorSpec inj, string fragment)
    {
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
        return name.StartsWith('.') || name is "Dockerfile" or "justfile" or "Tiltfile";
    }
}
