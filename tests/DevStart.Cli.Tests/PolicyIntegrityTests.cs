using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Every policy bundle must be internally consistent: name matches folder,
/// every validator declares a check kind we support, every extends target
/// exists, and every referenced fragment resolves.
/// </summary>
public class PolicyIntegrityTests
{
    private static readonly HashSet<string> KnownChecks =
        new(StringComparer.Ordinal) { "file-exists", "file-contains", "image-allowlist" };

    [Fact]
    public void Every_policy_is_consistent()
    {
        var names = Policy.AvailableNames().ToList();
        names.Should().NotBeEmpty();

        var issues = new List<string>();

        foreach (var name in names)
        {
            Policy policy;
            try { policy = Policy.LoadEmbedded(name); }
            catch (Exception ex) { issues.Add($"{name}: load failed — {ex.Message}"); continue; }

            if (policy.Name != name)
                issues.Add($"{name}: policy.json name='{policy.Name}' != folder='{name}'");

            foreach (var ext in policy.Extends)
            {
                if (!names.Contains(ext))
                    issues.Add($"{name}: extends '{ext}' not found");
            }

            foreach (var v in policy.Validators)
            {
                if (!KnownChecks.Contains(v.Check))
                    issues.Add($"{name}: validator '{v.Id}' uses unknown check '{v.Check}'");
            }

            foreach (var inj in policy.Injectors)
            {
                if (string.IsNullOrEmpty(inj.File))
                    issues.Add($"{name}: injector missing file");
                if (Policy.ReadFragment(name, inj.Fragment) is null)
                    issues.Add($"{name}: injector references missing fragment '{inj.Fragment}'");
            }
        }

        issues.Should().BeEmpty(because: string.Join(Environment.NewLine, issues));
    }
}
