using DevStart.Commands;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class PolicyTests
{
    [Fact]
    public void ResolveExtends_orders_bases_before_leaf()
    {
        var strict = Policy.LoadEmbedded("org-strict");
        var chain = PolicyCommand.ResolveExtends(strict).Select(p => p.Name).ToList();
        chain.Should().Equal("default-open-source", "org-strict");
    }

    [Fact]
    public void Validate_runs_inherited_validators()
    {
        // org-strict extends default-open-source; the OSS-baseline
        // codeql-required validator must still fire when validating strict.
        var strict = Policy.LoadEmbedded("org-strict");
        var validators = PolicyCommand.ResolveExtends(strict)
            .SelectMany(p => p.Validators.Select(v => v.Id))
            .ToList();
        validators.Should().Contain("codeql-required");
        validators.Should().Contain("commit-signing-required");
    }


    [Fact]
    public void Bundles_discoverable_by_name()
    {
        var names = Policy.AvailableNames().ToList();
        names.Should().Contain("default-open-source");
        names.Should().Contain("org-strict");
    }

    [Fact]
    public void Default_oss_policy_loads_with_validators()
    {
        var p = Policy.LoadEmbedded("default-open-source");
        p.Name.Should().Be("default-open-source");
        p.Validators.Should().NotBeEmpty();
        p.Validators.Select(v => v.Id).Should().Contain("codeql-required");
        p.Validators.Select(v => v.Id).Should().Contain("base-image-allowlist");
    }

    [Fact]
    public void Org_strict_policy_extends_default_and_adds_signing_check()
    {
        var p = Policy.LoadEmbedded("org-strict");
        p.Extends.Should().Contain("default-open-source");
        p.Validators.Select(v => v.Id).Should().Contain("commit-signing-required");
    }

    [Fact]
    public void Validator_runner_reports_missing_files_as_failures()
    {
        var tmp = Directory.CreateTempSubdirectory("devstart-policy-").FullName;
        try
        {
            var policy = Policy.LoadEmbedded("default-open-source");
            var results = PolicyValidatorRunner.Run(policy, tmp).ToList();
            results.Should().NotBeEmpty();
            results.Should().Contain(r => !r.Passed, "empty directory fails file-exists checks");
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Image_allowlist_rejects_unknown_base_image()
    {
        var tmp = Directory.CreateTempSubdirectory("devstart-policy-").FullName;
        try
        {
            File.WriteAllText(Path.Join(tmp, "Dockerfile"), "FROM ubuntu:22.04\nRUN true\n");
            var policy = Policy.LoadEmbedded("default-open-source");
            var results = PolicyValidatorRunner.Run(policy, tmp).ToList();
            results.Should().Contain(r => r.ValidatorId == "base-image-allowlist" && !r.Passed);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Image_allowlist_accepts_allowed_base_image()
    {
        var tmp = Directory.CreateTempSubdirectory("devstart-policy-").FullName;
        try
        {
            File.WriteAllText(Path.Join(tmp, "Dockerfile"), "FROM gcr.io/distroless/base:nonroot\n");
            var policy = Policy.LoadEmbedded("default-open-source");
            var results = PolicyValidatorRunner.Run(policy, tmp).ToList();
            results.Should().Contain(r => r.ValidatorId == "base-image-allowlist" && r.Passed);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }
}
