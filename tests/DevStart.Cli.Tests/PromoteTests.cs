using DevStart.Commands;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class PromoteTests
{
    [Theory]
    [InlineData("dev", "replicaCount: 1", "enabled: false")]
    [InlineData("stage", "replicaCount: 2", "enabled: true")]
    [InlineData("prod", "replicaCount: 3", "enabled: true")]
    public void BuildValues_sets_env_specific_replicas_and_hpa(string env, string replicas, string hpa)
    {
        var manifest = new Manifest
        {
            Name = "demo",
            Stack = Planner.StackDotnet,
            Capabilities = ["base", "postgres", "otel", "k8s"],
            Services = ["api"],
            Deploy = "none",
        };

        var values = PromoteCommand.BuildValues(env, manifest);

        values.Should().Contain(replicas);
        values.Should().Contain("hpa:");
        // hpa block should reflect the expected enabled state
        var hpaIdx = values.IndexOf("hpa:", StringComparison.Ordinal);
        values[hpaIdx..].Should().Contain(hpa);
    }

    [Fact]
    public void BuildValues_enables_migrations_when_postgres_installed()
    {
        var manifest = new Manifest
        {
            Name = "demo",
            Capabilities = ["base", "postgres", "k8s"],
            Services = ["api"],
        };
        var values = PromoteCommand.BuildValues("dev", manifest);
        values.Should().Contain("migrations:");
        var idx = values.IndexOf("migrations:", StringComparison.Ordinal);
        values[idx..].Should().Contain("enabled: true");
    }

    [Fact]
    public void BuildValues_enables_migrations_for_ts_postgres_too()
    {
        var manifest = new Manifest
        {
            Name = "demo",
            Stack = Planner.StackTypescript,
            Capabilities = ["ts-base", "ts-postgres", "k8s"],
            Services = ["api"],
        };
        var values = PromoteCommand.BuildValues("prod", manifest);
        var idx = values.IndexOf("migrations:", StringComparison.Ordinal);
        values[idx..].Should().Contain("enabled: true");
    }

    [Fact]
    public void BuildValues_disables_otel_when_not_installed()
    {
        var manifest = new Manifest
        {
            Name = "demo",
            Capabilities = ["base", "k8s"],
            Services = ["api"],
        };
        var values = PromoteCommand.BuildValues("dev", manifest);
        var idx = values.IndexOf("otel:", StringComparison.Ordinal);
        values[idx..].Should().Contain("enabled: false");
    }
}
