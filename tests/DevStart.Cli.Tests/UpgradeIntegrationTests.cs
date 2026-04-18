using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// End-to-end coverage for <c>dev-start upgrade --apply</c>: each test sets
/// up a realistic scenario (user edits, template drift, brand-new file,
/// removed file) against a fully-scaffolded project, runs the plan
/// builder + apply, and asserts the final disk state matches intent.
///
/// The "template" is simulated by producing two renders from the same
/// Planner into different directories, then hand-editing one of them so
/// a re-render detects drift. This exercises the real pipeline without
/// having to version-bump the embedded template between two builds.
/// </summary>
[Collection("SandboxCwd")]
public class UpgradeIntegrationTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;

    public UpgradeIntegrationTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-upgrade-int-").FullName;
        _priorCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_sandbox);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_priorCwd);
        try { Directory.Delete(_sandbox, recursive: true); }
        catch (IOException) { /* best-effort */ }
        catch (UnauthorizedAccessException) { /* best-effort */ }
        GC.SuppressFinalize(this);
    }

    private Planner MakePlanner(string name = "demo") => new(
        name,
        multiService: false,
        capabilities: ["postgres", "auth"],
        deployTarget: "none",
        includeClaude: false);

    [Fact]
    public async Task User_edited_file_is_preserved_when_template_unchanged()
    {
        // Scaffold a project.
        var planner = MakePlanner();
        await planner.RunAsync();
        var root = Path.Join(_sandbox, "demo");

        // User edits a base file.
        var programPath = Path.Join(root, "src/Demo.Api/Program.cs");
        var edited = File.ReadAllText(programPath) + "\n// user's custom note\n";
        File.WriteAllText(programPath, edited);

        // Re-render the SAME template into staging (no template change).
        var staging = Path.Join(_sandbox, "staging");
        planner.Render(staging);

        var plan = Upgrader.BuildPlan(root, staging, Baselines.Load(root));

        plan.UserEditsPreserved.Should().Contain("src/Demo.Api/Program.cs");
        plan.UpdatedCleanly.Should().NotContain("src/Demo.Api/Program.cs");
        plan.Conflicts.Should().NotContain("src/Demo.Api/Program.cs");

        Upgrader.ApplyPlan(plan, root, staging);
        File.ReadAllText(programPath).Should().EndWith("// user's custom note\n",
            "user edits must survive --apply for user-preserved files");
    }

    [Fact]
    public async Task Template_change_propagates_when_user_didnt_touch_file()
    {
        var planner = MakePlanner();
        await planner.RunAsync();
        var root = Path.Join(_sandbox, "demo");

        // Re-render, then mutate the staging version to simulate a template
        // change (since we can't actually bump the embedded template in-test).
        var staging = Path.Join(_sandbox, "staging");
        planner.Render(staging);
        var stagedProgram = Path.Join(staging, "src/Demo.Api/Program.cs");
        File.WriteAllText(stagedProgram, File.ReadAllText(stagedProgram) + "\n// template update\n");

        var plan = Upgrader.BuildPlan(root, staging, Baselines.Load(root));

        plan.UpdatedCleanly.Should().Contain("src/Demo.Api/Program.cs");
        plan.Conflicts.Should().NotContain("src/Demo.Api/Program.cs");

        Upgrader.ApplyPlan(plan, root, staging);
        File.ReadAllText(Path.Join(root, "src/Demo.Api/Program.cs"))
            .Should().EndWith("// template update\n");
    }

    [Fact]
    public async Task Divergent_changes_land_as_upgrade_preview_not_overwrite()
    {
        var planner = MakePlanner();
        await planner.RunAsync();
        var root = Path.Join(_sandbox, "demo");

        // Edit locally.
        var programPath = Path.Join(root, "src/Demo.Api/Program.cs");
        File.WriteAllText(programPath, File.ReadAllText(programPath) + "\n// user edit\n");

        // "Template" also changes.
        var staging = Path.Join(_sandbox, "staging");
        planner.Render(staging);
        var stagedProgram = Path.Join(staging, "src/Demo.Api/Program.cs");
        File.WriteAllText(stagedProgram, File.ReadAllText(stagedProgram) + "\n// template change\n");

        var plan = Upgrader.BuildPlan(root, staging, Baselines.Load(root));
        plan.Conflicts.Should().Contain("src/Demo.Api/Program.cs");

        Upgrader.ApplyPlan(plan, root, staging);

        File.ReadAllText(programPath).Should().EndWith("// user edit\n",
            "divergent changes must leave the user's version on disk untouched");
        File.Exists(programPath + ".upgrade-preview").Should().BeTrue(
            "template's version must land as a .upgrade-preview sibling");
        File.ReadAllText(programPath + ".upgrade-preview").Should().EndWith("// template change\n");
    }

    [Fact]
    public async Task New_template_file_lands_as_add()
    {
        var planner = MakePlanner();
        await planner.RunAsync();
        var root = Path.Join(_sandbox, "demo");

        var staging = Path.Join(_sandbox, "staging");
        planner.Render(staging);

        // Simulate a brand-new file appearing in the refreshed template.
        var newFileRel = "src/Demo.Api/NewHelper.cs";
        var newFileAbs = Path.Join(staging, newFileRel);
        File.WriteAllText(newFileAbs, "namespace Demo.Api; internal static class NewHelper { }\n");

        var plan = Upgrader.BuildPlan(root, staging, Baselines.Load(root));
        plan.Added.Should().Contain(newFileRel);

        Upgrader.ApplyPlan(plan, root, staging);
        File.Exists(Path.Join(root, newFileRel)).Should().BeTrue();
    }

    [Fact]
    public async Task File_removed_from_template_is_noted_but_not_deleted()
    {
        var planner = MakePlanner();
        await planner.RunAsync();
        var root = Path.Join(_sandbox, "demo");
        var baselines = Baselines.Load(root);

        var staging = Path.Join(_sandbox, "staging");
        planner.Render(staging);

        // Simulate the template dropping a file that the baseline remembers.
        var stillInRoot = "src/Demo.Api/Program.cs";
        var stagingAbs = Path.Join(staging, stillInRoot);
        File.Delete(stagingAbs);

        var plan = Upgrader.BuildPlan(root, staging, baselines);
        plan.RemovedFromTemplate.Should().Contain(stillInRoot,
            "the planner should note the drop");

        Upgrader.ApplyPlan(plan, root, staging);
        File.Exists(Path.Join(root, stillInRoot)).Should().BeTrue(
            "Apply must not delete user files — dropping is reported, not enforced");
    }

    [Fact]
    public async Task Idempotent_upgrade_against_unchanged_project_writes_nothing_destructive()
    {
        var planner = MakePlanner();
        await planner.RunAsync();
        var root = Path.Join(_sandbox, "demo");

        // Record mtimes before the upgrade; if Apply runs correctly over a
        // no-op plan, no file should be touched.
        var snapshot = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(p => p, File.GetLastWriteTimeUtc);

        var staging = Path.Join(_sandbox, "staging");
        planner.Render(staging);

        var plan = Upgrader.BuildPlan(root, staging, Baselines.Load(root));
        plan.Added.Should().BeEmpty();
        plan.UpdatedCleanly.Should().BeEmpty();
        plan.Conflicts.Should().BeEmpty();

        Upgrader.ApplyPlan(plan, root, staging);

        foreach (var (path, mtime) in snapshot)
        {
            File.GetLastWriteTimeUtc(path).Should().Be(mtime,
                $"{path} should not have been touched by a no-op upgrade");
        }
    }
}
