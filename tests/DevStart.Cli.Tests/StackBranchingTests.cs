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

    [Theory]
    [InlineData("skills/dotnet/add-endpoint.md", Planner.StackDotnet, "skills/add-endpoint.md")]
    [InlineData("skills/dotnet/add-endpoint.md", Planner.StackTypescript, null)]
    [InlineData("skills/typescript/add-endpoint.md", Planner.StackTypescript, "skills/add-endpoint.md")]
    [InlineData("skills/typescript/add-endpoint.md", Planner.StackDotnet, null)]
    [InlineData("agents/reviewer.md", Planner.StackDotnet, "agents/reviewer.md")]
    [InlineData("CLAUDE.md.dotnet.template", Planner.StackTypescript, "CLAUDE.md.dotnet.template")]
    public void RouteClaudePath_filters_by_stack(string input, string stack, string? expected)
    {
        Planner.RouteClaudePath(input, stack).Should().Be(expected);
    }
}
