using DevStart;
using DevStart.Install;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class InstallPlanTests
{
    private static CheckResult Failing(Capability.DoctorCheck check)
        => new(check, CheckOutcome.Failed, "[red]missing[/]");

    [Fact]
    public void Maps_service_check_to_service_action()
    {
        var plan = InstallPlan.Build(
            new[] { Failing(new Capability.DoctorCheck { Check = "service", Name = "postgres", Port = 5432 }) },
            new BrewPackageManager(),
            includeOptional: false);

        plan.Should().HaveCount(1);
        plan[0].Category.Should().Be(ActionCategory.Service);
        plan[0].Name.Should().Be("postgres");
        plan[0].Skipped.Should().BeFalse();
    }

    [Fact]
    public void Maps_dotnet_tool_check_to_dotnet_tool_install_command()
    {
        var plan = InstallPlan.Build(
            new[] { Failing(new Capability.DoctorCheck { Check = "dotnet-tool", Name = "dotnet-ef" }) },
            new AptPackageManager(),
            includeOptional: false);

        plan[0].Category.Should().Be(ActionCategory.DotnetTool);
        plan[0].Command.Should().Be("dotnet tool install -g dotnet-ef");
    }

    [Fact]
    public void Routes_node_through_runtime_category()
    {
        var plan = InstallPlan.Build(
            new[] { Failing(new Capability.DoctorCheck { Check = "tool", Name = "node" }) },
            new BrewPackageManager(),
            includeOptional: false);

        plan[0].Category.Should().Be(ActionCategory.Runtime);
        plan[0].Command.Should().Contain("node");
    }

    [Fact]
    public void Optional_checks_are_skipped_unless_include_optional_is_true()
    {
        var check = new Capability.DoctorCheck { Check = "tool", Name = "flyctl", Required = false };

        var withoutFlag = InstallPlan.Build(new[] { Failing(check) }, new BrewPackageManager(), includeOptional: false);
        withoutFlag.Should().HaveCount(1);
        withoutFlag[0].Skipped.Should().BeTrue();
        withoutFlag[0].Reason.Should().Contain("optional");

        var withFlag = InstallPlan.Build(new[] { Failing(check) }, new BrewPackageManager(), includeOptional: true);
        withFlag[0].Skipped.Should().BeFalse();
        withFlag[0].Command.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Unsupported_package_manager_falls_back_to_manual_url()
    {
        var plan = InstallPlan.Build(
            new[] { Failing(new Capability.DoctorCheck { Check = "tool", Name = "just" }) },
            new NullPackageManager(),
            includeOptional: false);

        plan[0].Skipped.Should().BeTrue();
        plan[0].Command.Should().BeNull();
        plan[0].ManualUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Env_check_is_surfaced_as_manual_with_doctor_fix_hint()
    {
        var plan = InstallPlan.Build(
            new[] { Failing(new Capability.DoctorCheck { Check = "env", Name = "DATABASE_URL" }) },
            new BrewPackageManager(),
            includeOptional: false);

        plan[0].Category.Should().Be(ActionCategory.Manual);
        plan[0].Skipped.Should().BeTrue();
        plan[0].Reason.Should().Contain("doctor --fix");
    }

    [Fact]
    public void Actions_ordered_runtime_then_tool_then_service_then_dotnet_tool()
    {
        var plan = InstallPlan.Build(
            new[]
            {
                Failing(new Capability.DoctorCheck { Check = "dotnet-tool", Name = "dotnet-ef" }),
                Failing(new Capability.DoctorCheck { Check = "service", Name = "postgres", Port = 5432 }),
                Failing(new Capability.DoctorCheck { Check = "tool", Name = "just" }),
                Failing(new Capability.DoctorCheck { Check = "tool", Name = "dotnet" }),
            },
            new BrewPackageManager(),
            includeOptional: false);

        plan.Select(a => a.Category).Should().ContainInOrder(
            ActionCategory.Runtime, ActionCategory.Tool, ActionCategory.Service, ActionCategory.DotnetTool);
    }

    [Fact]
    public void Unknown_tool_yields_skipped_action()
    {
        var plan = InstallPlan.Build(
            new[] { Failing(new Capability.DoctorCheck { Check = "tool", Name = "totally-fake-tool" }) },
            new BrewPackageManager(),
            includeOptional: false);

        plan[0].Skipped.Should().BeTrue();
        plan[0].Reason.Should().Contain("not in tool catalog");
    }

    [Fact]
    public void Service_alias_normalizes_mailhog_smtp()
    {
        ServiceStarter.Normalize("mailhog-smtp").Should().Be("mailhog");
        ServiceStarter.Normalize("postgres").Should().Be("postgres");
    }
}
