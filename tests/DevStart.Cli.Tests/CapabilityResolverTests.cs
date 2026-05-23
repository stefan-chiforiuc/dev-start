using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class CapabilityResolverTests
{
    [Fact]
    public void Exact_match_wins_for_legacy_prefixed_name()
    {
        var resolved = CapabilityResolver.Resolve(
            new CapabilityResolver.Selection("ts-auth", Planner.StackTypescript));
        resolved.Should().Be("ts-auth");
    }

    [Fact]
    public void Flat_name_in_dotnet_project_resolves_to_dotnet_capability()
    {
        var resolved = CapabilityResolver.Resolve(
            new CapabilityResolver.Selection("auth", Planner.StackDotnet));
        resolved.Should().Be("auth");
    }

    [Fact]
    public void Flat_name_in_typescript_project_resolves_to_ts_prefixed_capability()
    {
        var resolved = CapabilityResolver.Resolve(
            new CapabilityResolver.Selection("auth", Planner.StackTypescript));
        resolved.Should().Be("ts-auth");
    }

    [Fact]
    public void S3_in_typescript_project_resolves_to_ts_s3()
    {
        var resolved = CapabilityResolver.Resolve(
            new CapabilityResolver.Selection("s3", Planner.StackTypescript));
        resolved.Should().Be("ts-s3");
    }

    [Fact]
    public void Unknown_capability_returns_null()
    {
        var resolved = CapabilityResolver.Resolve(
            new CapabilityResolver.Selection("does-not-exist", Planner.StackDotnet));
        resolved.Should().BeNull();
    }

    [Fact]
    public void ResolveDeploy_with_fly_returns_deploy_fly_for_dotnet()
    {
        CapabilityResolver.ResolveDeploy(Planner.StackDotnet, "fly").Should().Be("deploy-fly");
    }

    [Fact]
    public void ResolveDeploy_with_fly_returns_ts_deploy_fly_for_typescript()
    {
        CapabilityResolver.ResolveDeploy(Planner.StackTypescript, "fly").Should().Be("ts-deploy-fly");
    }

    [Fact]
    public void ResolveDeploy_with_aca_returns_deploy_aca()
    {
        CapabilityResolver.ResolveDeploy(Planner.StackDotnet, "aca").Should().Be("deploy-aca");
        CapabilityResolver.ResolveDeploy(Planner.StackTypescript, "aca").Should().Be("ts-deploy-aca");
    }

    [Fact]
    public void ResolveDeploy_returns_null_for_none_or_empty()
    {
        CapabilityResolver.ResolveDeploy(Planner.StackDotnet, "none").Should().BeNull();
        CapabilityResolver.ResolveDeploy(Planner.StackDotnet, "").Should().BeNull();
    }

    [Fact]
    public void ResolveBackend_returns_base_for_dotnet()
    {
        CapabilityResolver.ResolveBackend(Planner.StackDotnet, null, null).Should().Be("base");
    }

    [Fact]
    public void ResolveBackend_returns_ts_base_for_typescript()
    {
        CapabilityResolver.ResolveBackend(Planner.StackTypescript, null, null).Should().Be("ts-base");
    }

    [Fact]
    public void ResolveBackend_filters_by_framework_when_specified()
    {
        CapabilityResolver.ResolveBackend(Planner.StackDotnet, "aspnet", null).Should().Be("base");
        CapabilityResolver.ResolveBackend(Planner.StackTypescript, "fastify", null).Should().Be("ts-base");
    }

    [Fact]
    public void BuildAliasMap_maps_base_provider_to_concrete_variant()
    {
        var aliases = CapabilityResolver.BuildAliasMap(new[] { "base", "postgres", "auth" });
        aliases.Should().ContainKey("base").WhoseValue.Should().Be("base");
    }

    [Fact]
    public void BuildAliasMap_includes_ts_base_alias_to_base()
    {
        // ts-base declares `provides: ["ts-base", "base"]` so dependents
        // written for .NET (`dependsOn: ["base"]`) work without churn.
        var aliases = CapabilityResolver.BuildAliasMap(new[] { "ts-base" });
        aliases.Should().ContainKey("base").WhoseValue.Should().Be("ts-base");
        aliases.Should().ContainKey("ts-base").WhoseValue.Should().Be("ts-base");
    }

    [Fact]
    public void ListFamily_backend_returns_variants_filtered_by_stack()
    {
        var dotnetBackends = CapabilityResolver.ListFamily("backend", Planner.StackDotnet);
        dotnetBackends.Should().Contain(c => c.Name == "base");
        dotnetBackends.Should().NotContain(c => c.Name == "ts-base");

        var tsBackends = CapabilityResolver.ListFamily("backend", Planner.StackTypescript);
        tsBackends.Should().Contain(c => c.Name == "ts-base");
        tsBackends.Should().NotContain(c => c.Name == "base");
    }

    [Fact]
    public void ListFamily_deploy_returns_both_targets_per_stack()
    {
        var dotnetDeploy = CapabilityResolver.ListFamily("deploy", Planner.StackDotnet);
        dotnetDeploy.Select(c => c.Name).Should().BeEquivalentTo(["deploy-fly", "deploy-aca"]);

        var tsDeploy = CapabilityResolver.ListFamily("deploy", Planner.StackTypescript);
        tsDeploy.Select(c => c.Name).Should().BeEquivalentTo(["ts-deploy-fly", "ts-deploy-aca"]);
    }
}
