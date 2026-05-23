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
}
