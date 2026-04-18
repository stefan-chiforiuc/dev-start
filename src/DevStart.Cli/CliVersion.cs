using System.Reflection;

namespace DevStart;

/// <summary>
/// Exposes the version baked into the CLI assembly. Used by <see cref="Planner"/>
/// and <c>dev-start upgrade --apply</c> to stamp <c>.devstart.json</c>'s
/// <c>templateVersion</c>, so a project always knows which template bits it
/// was last rendered with.
/// </summary>
public static class CliVersion
{
    /// <summary>Informational version string (from <c>AssemblyInformationalVersionAttribute</c>).</summary>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var asm = Assembly.GetExecutingAssembly();
        // AssemblyInformationalVersion is the friendliest form — it honours
        // SourceLink / git height suffixes. Fall back to assembly version.
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            // MSBuild appends +<commitSha> when SourceLink is on; strip for a clean version.
            var plus = info.IndexOf('+', StringComparison.Ordinal);
            return plus > 0 ? info[..plus] : info;
        }
        return asm.GetName().Version?.ToString() ?? "0.0.0";
    }
}
