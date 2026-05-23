namespace DevStart;

/// <summary>
/// Maps user-facing capability names (e.g. <c>auth</c>, <c>deploy</c>) to the
/// concrete capability folder on disk (e.g. <c>ts-auth</c>, <c>deploy-fly</c>)
/// given the current project's stack and any family parameters.
///
/// Resolution order (first match wins):
/// <list type="number">
///   <item>Exact folder match (preserves the legacy <c>ts-*</c> names as an
///         escape hatch — explicit always wins).</item>
///   <item>Stack-prefixed match: in TS projects, <c>auth</c> → <c>ts-auth</c>.</item>
///   <item>Family resolution: <c>deploy</c> + <c>target=fly</c> →
///         <c>deploy-fly</c> (+ ts- prefix per stack).</item>
/// </list>
/// See ADR 0010.
/// </summary>
public static class CapabilityResolver
{
    public sealed record Selection(
        string Name,                 // user-typed name (e.g. "auth", "deploy")
        string Stack,                // project stack
        string? FamilyTarget = null, // e.g. --target fly | --framework aspnet
        string? FamilyVersion = null // e.g. --framework-version 9
    );

    /// <summary>
    /// Resolve a selection to a concrete capability folder name, or null when
    /// no candidate exists. Callers print their own error message.
    /// </summary>
    public static string? Resolve(Selection sel)
    {
        // 0. If the user pinned a family parameter (engine, target,
        //    framework, version), family resolution takes precedence over
        //    exact-name match. Otherwise `add cache --engine memory` would
        //    short-circuit to the bare `cache` folder via rule 2.
        if (sel.FamilyTarget is not null || sel.FamilyVersion is not null)
        {
            var pinned = ResolveFamily(sel);
            if (pinned is not null) return pinned;
        }

        // 1. In TS projects, the prefixed sibling wins over the bare name
        //    (bare `auth` is the .NET capability — we don't want to install
        //    it into a TS project). Typing `ts-auth` explicitly still works
        //    because it'll exact-match in rule 2.
        if (sel.Stack == Planner.StackTypescript
            && !sel.Name.StartsWith("ts-", StringComparison.Ordinal))
        {
            var prefixed = "ts-" + sel.Name;
            if (CapabilityExists(prefixed))
            {
                return prefixed;
            }
        }

        // 2. Exact match — preserves explicit names like `ts-auth` or
        //    `deploy-fly` as power-user escape hatches. Stack guarding
        //    happens later in AddCommand.
        if (CapabilityExists(sel.Name))
        {
            // In a TS project, refuse to silently match a bare .NET capability
            // (e.g. `add auth` when no `ts-auth` exists is a user error).
            if (sel.Stack == Planner.StackTypescript
                && !sel.Name.StartsWith("ts-", StringComparison.Ordinal))
            {
                var cap = TryLoad(sel.Name);
                var crossStack = cap?.DependsOnByStack is not null
                    || (cap?.Stacks.Contains(Planner.StackTypescript, StringComparer.Ordinal) ?? false)
                    || (cap?.Family is not null);
                if (!crossStack) return null;
            }
            return sel.Name;
        }

        // 3. Family resolution. The user typed a family root (e.g. "deploy"
        //    or "backend"); pick the variant that matches the target/version.
        var familyMatch = ResolveFamily(sel);
        if (familyMatch is not null) return familyMatch;

        return null;
    }

    private static Capability? TryLoad(string name)
    {
        try { return Capability.LoadEmbedded(name); }
        catch { return null; }
    }

    /// <summary>
    /// Concrete folder name for the project's backend foundation. Today this
    /// is just <c>base</c> or <c>ts-base</c>; once per-version folders ship
    /// it'll branch on framework + version.
    /// </summary>
    public static string ResolveBackend(string stack, string? framework, string? version)
    {
        var sel = new Selection("backend", stack, framework, version);
        var concrete = ResolveFamily(sel);
        if (concrete is not null) return concrete;

        // Fallback for legacy capability set without `family: backend`.
        return stack == Planner.StackTypescript ? "ts-base" : "base";
    }

    /// <summary>
    /// Concrete folder name for the deploy target, or null when target is
    /// none/unknown. Returns <c>ts-deploy-fly</c> in a TS project.
    /// </summary>
    public static string? ResolveDeploy(string stack, string target)
    {
        if (string.IsNullOrWhiteSpace(target) || target.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalized = NormalizeDeployTarget(target);
        if (normalized is null) return null;

        var sel = new Selection("deploy", stack, FamilyTarget: normalized);
        var concrete = ResolveFamily(sel);
        if (concrete is not null) return concrete;

        // Fallback: legacy name shapes when `family: deploy` isn't yet set
        // on the capability JSON files.
        var legacy = "deploy-" + normalized;
        if (stack == Planner.StackTypescript)
        {
            var tsLegacy = "ts-" + legacy;
            if (CapabilityExists(tsLegacy)) return tsLegacy;
        }
        return CapabilityExists(legacy) ? legacy : null;
    }

    /// <summary>
    /// Build the alias map for a Planner run: every concrete capability that
    /// declares <c>provides</c> contributes one entry per provided name.
    /// Used so dependents can declare <c>dependsOn: ["base"]</c> while the
    /// installed folder is a specific variant.
    /// </summary>
    public static Dictionary<string, string> BuildAliasMap(IEnumerable<string> installedNames)
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in installedNames)
        {
            Capability cap;
            try { cap = Capability.LoadEmbedded(name); }
            catch { continue; }

            foreach (var alias in cap.Provides)
            {
                if (!aliases.ContainsKey(alias))
                {
                    aliases[alias] = name;
                }
            }
        }
        return aliases;
    }

    /// <summary>
    /// Resolve a dependency name through the alias map (e.g. <c>base</c> →
    /// <c>base-aspnet-9</c>). Unknown names pass through unchanged.
    /// </summary>
    public static string ApplyAliases(string dep, IReadOnlyDictionary<string, string> aliases)
    {
        return aliases.TryGetValue(dep, out var concrete) ? concrete : dep;
    }

    /// <summary>
    /// All known framework + version variants within a family, filtered by
    /// stack. Used by the wizard to populate the framework/version prompts.
    /// </summary>
    public static IReadOnlyList<Capability> ListFamily(string family, string stack)
    {
        var matches = new List<Capability>();
        foreach (var name in Capability.AvailableNames())
        {
            Capability cap;
            try { cap = Capability.LoadEmbedded(name); }
            catch { continue; }

            if (!string.Equals(cap.Family, family, StringComparison.Ordinal)) continue;
            if (cap.Stacks.Count > 0 && !cap.Stacks.Contains(stack, StringComparer.Ordinal)) continue;
            matches.Add(cap);
        }
        return matches;
    }

    private static string? ResolveFamily(Selection sel)
    {
        // Collect every candidate in the family that matches stack +
        // optional target/version filters, then pick the best per the
        // documented tie-break rules: explicit `default: true` wins;
        // otherwise the highest framework version.
        var candidates = new List<Capability>();
        foreach (var name in Capability.AvailableNames())
        {
            Capability cap;
            try { cap = Capability.LoadEmbedded(name); }
            catch { continue; }

            if (!string.Equals(cap.Family, sel.Name, StringComparison.Ordinal)) continue;
            if (cap.Stacks.Count > 0 && !cap.Stacks.Contains(sel.Stack, StringComparer.Ordinal)) continue;

            // Skip TS-prefixed folders when the project is .NET (their
            // capability.json should already declare `stacks` but be defensive).
            if (sel.Stack != Planner.StackTypescript && name.StartsWith("ts-", StringComparison.Ordinal))
                continue;
            if (sel.Stack == Planner.StackTypescript && cap.Stacks.Count == 0
                && !name.StartsWith("ts-", StringComparison.Ordinal))
                continue;

            if (sel.FamilyTarget is not null
                && !string.Equals(cap.Framework, sel.FamilyTarget, StringComparison.OrdinalIgnoreCase))
                continue;

            if (sel.FamilyVersion is not null
                && !string.Equals(cap.FrameworkVersion, sel.FamilyVersion, StringComparison.OrdinalIgnoreCase))
                continue;

            candidates.Add(cap);
        }

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0].Name;

        // No version pinned — prefer an explicit default, fall back to the
        // highest version. Picking the LTS as default is the convention.
        var explicitDefault = candidates.FirstOrDefault(c => c.Default);
        if (explicitDefault is not null) return explicitDefault.Name;

        return candidates
            .OrderByDescending(c => c.FrameworkVersion, VersionComparer.Instance)
            .First().Name;
    }

    private sealed class VersionComparer : IComparer<string?>
    {
        public static readonly VersionComparer Instance = new();
        public int Compare(string? a, string? b) => CompareVersions(a, b);
    }

    private static int CompareVersions(string? a, string? b)
    {
        if (a is null && b is null) return 0;
        if (a is null) return -1;
        if (b is null) return 1;
        // Numeric prefix compare ("10" > "9"), fall back to ordinal.
        if (int.TryParse(a, out var ai) && int.TryParse(b, out var bi))
            return ai.CompareTo(bi);
        return string.CompareOrdinal(a, b);
    }

    private static string? NormalizeDeployTarget(string target) => target.ToLowerInvariant() switch
    {
        "fly" or "flyio" or "fly.io" => "fly",
        "aca" or "azure" or "azurecontainerapps" => "aca",
        _ => null,
    };

    private static bool CapabilityExists(string name)
    {
        try { _ = Capability.LoadEmbedded(name); return true; }
        catch { return false; }
    }
}
