namespace DevStart;

/// <summary>
/// Pure 3-way-merge logic for <c>dev-start upgrade --apply</c>. Kept in its
/// own class so tests can drive it without going through
/// <see cref="Commands.UpgradeCommand"/>'s <see cref="System.CommandLine"/>
/// handler.
/// </summary>
public static class Upgrader
{
    /// <summary>
    /// Walk the staging tree and decide, per file, what should happen when
    /// the user runs <c>upgrade --apply</c>.
    /// </summary>
    /// <param name="projectRoot">The live user project directory.</param>
    /// <param name="stagingRoot">A fresh render of the current template.</param>
    /// <param name="baselines">
    /// What the template produced last time it wrote each file. Used to tell
    /// user edits apart from template updates.
    /// </param>
    public static UpgradePlan BuildPlan(string projectRoot, string stagingRoot, Baselines baselines)
    {
        var added = new List<string>();
        var updatedCleanly = new List<string>();
        var unchanged = new List<string>();
        var userPreserved = new List<string>();
        var conflicts = new List<string>();
        var removed = new List<string>();

        foreach (var stagedAbs in Directory.EnumerateFiles(stagingRoot, "*", SearchOption.AllDirectories))
        {
            var rel = Normalize(Path.GetRelativePath(stagingRoot, stagedAbs));
            var diskPath = Path.Join(projectRoot, rel);
            var stagedBytes = File.ReadAllBytes(stagedAbs);
            var stagedHash = Baselines.Hash(stagedBytes);

            if (!File.Exists(diskPath))
            {
                added.Add(rel);
                continue;
            }

            var diskBytes = File.ReadAllBytes(diskPath);
            var diskHash = Baselines.Hash(diskBytes);

            if (diskHash == stagedHash)
            {
                unchanged.Add(rel);
                continue;
            }

            var baseHash = baselines.Get(rel);
            if (baseHash is null)
            {
                // File exists on disk but we never baselined it. Could be a
                // user-authored file colliding with a template add — treat
                // conservatively as a conflict.
                conflicts.Add(rel);
                continue;
            }

            if (diskHash == baseHash)
            {
                // User hasn't touched this file; template moved forward.
                updatedCleanly.Add(rel);
            }
            else if (stagedHash == baseHash)
            {
                // Template unchanged for this file; user edited it. Keep.
                userPreserved.Add(rel);
            }
            else
            {
                // Both sides diverged from baseline. Manual merge.
                conflicts.Add(rel);
            }
        }

        // Files the baseline remembers but that no longer exist in staging —
        // capability was removed or template trimmed. We don't delete them
        // (the user may have come to depend on them); just report.
        foreach (var key in baselines.Files.Keys)
        {
            var stagedAbs = Path.Join(stagingRoot, key);
            if (!File.Exists(stagedAbs)) removed.Add(key);
        }

        return new UpgradePlan(added, updatedCleanly, unchanged, userPreserved, conflicts, removed);
    }

    /// <summary>
    /// Materialise the plan on disk: overwrite clean add/update files,
    /// leave user-edited files alone, land conflicts as
    /// <c>&lt;path&gt;.upgrade-preview</c> siblings the user can diff.
    /// </summary>
    public static void ApplyPlan(UpgradePlan plan, string projectRoot, string stagingRoot)
    {
        foreach (var rel in plan.Added.Concat(plan.UpdatedCleanly))
        {
            var src = Path.Join(stagingRoot, rel);
            var dst = Path.Join(projectRoot, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(src, dst, overwrite: true);
        }

        foreach (var rel in plan.Conflicts)
        {
            var src = Path.Join(stagingRoot, rel);
            var preview = Path.Join(projectRoot, rel) + ".upgrade-preview";
            Directory.CreateDirectory(Path.GetDirectoryName(preview)!);
            File.Copy(src, preview, overwrite: true);
        }
    }

    private static string Normalize(string p) => p.Replace('\\', '/');
}

/// <summary>
/// Outcome of <see cref="Upgrader.BuildPlan"/>. Each list holds relative
/// paths (forward-slash separated) of files in the project.
/// </summary>
public sealed record UpgradePlan(
    List<string> Added,
    List<string> UpdatedCleanly,
    List<string> UnchangedOnBothSides,
    List<string> UserEditsPreserved,
    List<string> Conflicts,
    List<string> RemovedFromTemplate);
