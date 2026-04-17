using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Keeps every capability internally consistent: name matches folder,
/// dependsOn / conflictsWith reference real capabilities, every injector
/// references an existing fragment file, and every injector declares
/// either a marker or an anchor.
/// </summary>
public class CapabilityIntegrityTests
{
    [Fact]
    public void Every_capability_is_consistent()
    {
        var names = Capability.AvailableNames().ToList();
        names.Should().NotBeEmpty(because: "the embedded resources must ship at least one capability");

        var issues = new List<string>();

        foreach (var name in names)
        {
            Capability cap;
            try
            {
                cap = Capability.LoadEmbedded(name);
            }
            catch (Exception ex)
            {
                issues.Add($"{name}: LoadEmbedded threw — {ex.Message}");
                continue;
            }

            if (cap.Name != name)
            {
                issues.Add($"{name}: capability.json name='{cap.Name}' != folder='{name}'");
            }

            foreach (var dep in cap.DependsOn)
            {
                if (!names.Contains(dep))
                {
                    issues.Add($"{name}: dependsOn '{dep}' not found");
                }
            }

            foreach (var conflict in cap.ConflictsWith)
            {
                if (!names.Contains(conflict))
                {
                    issues.Add($"{name}: conflictsWith '{conflict}' not found");
                }
            }

            var injectors = Capability.LoadInjectors(name);
            for (var i = 0; i < injectors.Injectors.Count; i++)
            {
                var inj = injectors.Injectors[i];
                if (string.IsNullOrEmpty(inj.Marker) && string.IsNullOrEmpty(inj.Anchor))
                {
                    issues.Add($"{name}: injector[{i}] has neither marker nor anchor");
                }
                if (string.IsNullOrEmpty(inj.File))
                {
                    issues.Add($"{name}: injector[{i}] missing target file");
                }
                if (Capability.ReadFragment(name, inj.Fragment) is null)
                {
                    issues.Add($"{name}: injector[{i}] references missing fragment '{inj.Fragment}'");
                }
                if (inj.Placement is not ("before" or "after" or "replace"))
                {
                    issues.Add($"{name}: injector[{i}] has unsupported placement '{inj.Placement}'");
                }
            }
        }

        issues.Should().BeEmpty(
            because: string.Join(Environment.NewLine, issues));
    }

    [Fact]
    public void Deploy_capabilities_do_not_conflict_with_non_deploy_siblings()
    {
        foreach (var name in Capability.AvailableNames())
        {
            var cap = Capability.LoadEmbedded(name);
            if (!name.StartsWith("deploy-", StringComparison.Ordinal)) continue;

            foreach (var conflict in cap.ConflictsWith)
            {
                conflict.Should().StartWith("deploy-",
                    because: $"{name} declares a conflict with '{conflict}', but only other deploy-* targets should conflict with each other");
            }
        }
    }
}
