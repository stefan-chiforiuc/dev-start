using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class CapabilityTests
{
    [Fact]
    public void AvailableNames_includes_base_and_postgres()
    {
        var names = Capability.AvailableNames().ToArray();
        names.Should().Contain("base");
        names.Should().Contain("postgres");
        names.Should().NotContain("_skeleton");
    }

    [Fact]
    public void LoadEmbedded_base_has_description()
    {
        var cap = Capability.LoadEmbedded("base");
        cap.Name.Should().Be("base");
        cap.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Postgres_depends_on_base()
    {
        var cap = Capability.LoadEmbedded("postgres");
        cap.DependsOn.Should().Contain("base");
    }

    [Fact]
    public void Every_capability_has_a_well_formed_manifest()
    {
        foreach (var name in Capability.AvailableNames())
        {
            var cap = Capability.LoadEmbedded(name);
            cap.Name.Should().Be(name, because: $"capability '{name}' has a mismatched name field");
            cap.Version.Should().NotBeNullOrWhiteSpace();
            cap.Description.Should().NotBeNullOrWhiteSpace();
        }
    }
}
