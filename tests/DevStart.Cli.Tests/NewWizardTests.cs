using DevStart.Wizard;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class NewWizardTests
{
    [Fact]
    public void Non_interactive_with_empty_preset_uses_dotnet_defaults()
    {
        var wizard = new NewWizard();
        var answers = wizard.Run(new NewWizard.Preset(), interactive: false);

        answers.Stack.Should().Be(Planner.StackDotnet);
        answers.Framework.Should().Be("aspnet");
        answers.FrameworkVersion.Should().Be("8");
        answers.Extras.Should().BeEquivalentTo(["postgres", "auth", "otel"]);
        answers.DeployTarget.Should().Be("none");
        answers.IncludeClaude.Should().BeTrue();
        answers.MultiService.Should().BeFalse();
    }

    [Fact]
    public void Non_interactive_with_typescript_stack_picks_fastify_defaults()
    {
        var wizard = new NewWizard();
        var answers = wizard.Run(new NewWizard.Preset(Stack: "typescript"), interactive: false);

        answers.Stack.Should().Be(Planner.StackTypescript);
        answers.Framework.Should().Be("fastify");
        answers.FrameworkVersion.Should().Be("5");
    }

    [Fact]
    public void Preset_values_pass_through()
    {
        var wizard = new NewWizard();
        var answers = wizard.Run(new NewWizard.Preset(
            Stack: "dotnet",
            Framework: "aspnet",
            FrameworkVersion: "8",
            Extras: ["postgres", "cache"],
            DeployTarget: "fly",
            IncludeClaude: false,
            MultiService: true), interactive: false);

        answers.DeployTarget.Should().Be("fly");
        answers.Extras.Should().BeEquivalentTo(["postgres", "cache"]);
        answers.IncludeClaude.Should().BeFalse();
        answers.MultiService.Should().BeTrue();
    }

    [Fact]
    public void Cache_engine_preset_is_preserved()
    {
        var wizard = new NewWizard();
        var answers = wizard.Run(new NewWizard.Preset(
            Stack: "dotnet",
            Extras: ["cache"],
            CacheEngine: "memory"), interactive: false);

        answers.CacheEngine.Should().Be("memory");
    }

    [Fact]
    public void Cache_in_extras_without_preset_falls_back_to_redis_default_non_interactive()
    {
        // No `default: true` flag on cache → the non-interactive picker
        // returns the first registered variant. Today that's redis; the
        // test locks in the contract that "no preset, non-interactive,
        // multiple variants" doesn't crash.
        var wizard = new NewWizard();
        var answers = wizard.Run(new NewWizard.Preset(
            Stack: "dotnet",
            Extras: ["cache"]), interactive: false);

        answers.CacheEngine.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Backend_version_default_is_LTS_not_highest()
    {
        // base (.NET 8) is flagged default; base-aspnet-9 isn't.
        var wizard = new NewWizard();
        var answers = wizard.Run(new NewWizard.Preset(Stack: "dotnet"), interactive: false);
        answers.FrameworkVersion.Should().Be("8");
    }
}
