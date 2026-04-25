using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class StackBranchingTests
{
    [Fact]
    public void Typescript_stack_uses_ts_base()
    {
        var planner = new Planner(
            name: "demo",
            multiService: false,
            capabilities: ["ts-postgres"],
            deployTarget: "none",
            includeClaude: false,
            stack: Planner.StackTypescript);

        planner.Capabilities.Should().StartWith("ts-base");
        planner.Capabilities.Should().Contain("ts-postgres");
    }

    [Fact]
    public void Dotnet_stack_uses_base()
    {
        var planner = new Planner(
            name: "demo",
            multiService: false,
            capabilities: ["postgres"],
            deployTarget: "none",
            includeClaude: false);

        planner.Capabilities.Should().StartWith("base");
        planner.Capabilities.Should().Contain("postgres");
    }

    [Fact]
    public void Multi_service_on_typescript_stack_adds_ts_gateway()
    {
        var planner = new Planner(
            name: "demo",
            multiService: true,
            capabilities: [],
            deployTarget: "none",
            includeClaude: false,
            stack: Planner.StackTypescript);

        planner.Capabilities.Should().Contain("ts-gateway");
    }

    [Fact]
    public void Fly_deploy_on_typescript_stack_picks_ts_deploy_fly()
    {
        var planner = new Planner(
            name: "demo",
            multiService: false,
            capabilities: [],
            deployTarget: "fly",
            includeClaude: false,
            stack: Planner.StackTypescript);

        planner.Capabilities.Should().Contain("ts-deploy-fly");
    }

    [Fact]
    public void NormalizeStack_accepts_common_aliases()
    {
        Planner.NormalizeStack("dotnet").Should().Be(Planner.StackDotnet);
        Planner.NormalizeStack("csharp").Should().Be(Planner.StackDotnet);
        Planner.NormalizeStack("typescript").Should().Be(Planner.StackTypescript);
        Planner.NormalizeStack("ts").Should().Be(Planner.StackTypescript);
        Planner.NormalizeStack("fastify").Should().Be(Planner.StackTypescript);
    }

    [Fact]
    public void Frontend_on_typescript_pulls_in_ts_sdk_transitively()
    {
        var planner = new Planner(
            name: "demo",
            multiService: false,
            capabilities: ["frontend"],
            deployTarget: "none",
            includeClaude: false,
            stack: Planner.StackTypescript);

        // frontend's dependsOnByStack[typescript-fastify] = ["ts-base", "ts-sdk"]
        planner.Capabilities.Should().Contain("ts-base");
        planner.Capabilities.Should().Contain("ts-sdk");
        planner.Capabilities.Should().Contain("frontend");
    }

    [Fact]
    public void Frontend_on_dotnet_pulls_in_sdk_transitively()
    {
        var planner = new Planner(
            name: "demo",
            multiService: false,
            capabilities: ["frontend"],
            deployTarget: "none",
            includeClaude: false);

        planner.Capabilities.Should().Contain("base");
        planner.Capabilities.Should().Contain("sdk");
        planner.Capabilities.Should().Contain("frontend");
    }

    [Fact]
    public void Transitive_resolution_places_base_before_leaf()
    {
        var resolved = Planner.ResolveTransitively(
            new List<string> { "postgres" }, Planner.StackDotnet);
        var baseIdx = resolved.ToList().IndexOf("base");
        var pgIdx = resolved.ToList().IndexOf("postgres");
        baseIdx.Should().BeLessThan(pgIdx);
    }

    [Theory]
    [InlineData("skills/dotnet/add-endpoint.md", Planner.StackDotnet, "skills/add-endpoint.md")]
    [InlineData("skills/dotnet/add-endpoint.md", Planner.StackTypescript, null)]
    [InlineData("skills/typescript/add-endpoint.md", Planner.StackTypescript, "skills/add-endpoint.md")]
    [InlineData("skills/typescript/add-endpoint.md", Planner.StackDotnet, null)]
    [InlineData("commands/dotnet/add-endpoint.md", Planner.StackDotnet, "commands/add-endpoint.md")]
    [InlineData("commands/typescript/add-endpoint.md", Planner.StackTypescript, "commands/add-endpoint.md")]
    [InlineData("commands/typescript/add-endpoint.md", Planner.StackDotnet, null)]
    [InlineData("agents/dotnet/reviewer.md", Planner.StackDotnet, "agents/reviewer.md")]
    [InlineData("agents/reviewer.md", Planner.StackDotnet, "agents/reviewer.md")]
    [InlineData("CLAUDE.md.dotnet.template", Planner.StackDotnet, "CLAUDE.md.dotnet.template")]
    [InlineData("CLAUDE.md.dotnet.template", Planner.StackTypescript, null)]
    [InlineData("CLAUDE.md.typescript.template", Planner.StackTypescript, "CLAUDE.md.typescript.template")]
    [InlineData("CLAUDE.md.typescript.template", Planner.StackDotnet, null)]
    [InlineData("settings.json", Planner.StackDotnet, "settings.json")]
    public void RouteClaudePath_filters_by_stack(string input, string stack, string? expected)
    {
        Planner.RouteClaudePath(input, stack).Should().Be(expected);
    }
}
