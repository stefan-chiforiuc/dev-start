using DevStart.Install;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class ToolCatalogTests
{
    [Theory]
    [InlineData("dotnet")]
    [InlineData("node")]
    [InlineData("pnpm")]
    [InlineData("just")]
    [InlineData("bash")]
    [InlineData("docker")]
    [InlineData("flyctl")]
    [InlineData("az")]
    [InlineData("git")]
    public void Lookup_returns_entry_for_every_seeded_tool(string name)
    {
        var entry = ToolCatalog.Lookup(name);
        entry.Should().NotBeNull();
        entry!.ManualUrl.Should().NotBeNullOrEmpty($"every entry needs a usable fallback URL ({name})");
    }

    [Theory]
    [InlineData("Dotnet")]
    [InlineData("DOCKER")]
    public void Lookup_is_case_insensitive(string name)
    {
        ToolCatalog.Lookup(name).Should().NotBeNull();
    }

    [Fact]
    public void Lookup_unknown_returns_null()
    {
        ToolCatalog.Lookup("ghostbusters").Should().BeNull();
    }

    [Fact]
    public void Brew_resolves_dotnet_to_dotnet_sdk_cask()
    {
        var pm = new BrewPackageManager();
        var entry = ToolCatalog.Lookup("dotnet")!;
        pm.BuildInstallCommand(entry).Should().Contain("brew install").And.Contain("dotnet-sdk");
    }

    [Fact]
    public void Apt_pnpm_has_no_native_package_so_command_is_null()
    {
        // pnpm isn't shipped in apt — install must fall through to manual URL.
        var pm = new AptPackageManager();
        var entry = ToolCatalog.Lookup("pnpm")!;
        pm.BuildInstallCommand(entry).Should().BeNull();
        entry.ManualUrl.Should().Contain("pnpm.io");
    }

    [Fact]
    public void Winget_bash_points_at_git_for_windows()
    {
        // Bash on Windows ships via Git for Windows; verifies the explicit choice.
        var pm = new WingetPackageManager();
        var entry = ToolCatalog.Lookup("bash")!;
        var cmd = pm.BuildInstallCommand(entry);
        cmd.Should().NotBeNull().And.Contain("Git.Git");
    }

    [Fact]
    public void Null_package_manager_never_produces_a_command()
    {
        var pm = new NullPackageManager();
        var entry = ToolCatalog.Lookup("just")!;
        pm.BuildInstallCommand(entry).Should().BeNull();
    }

    [Fact]
    public void Factory_picks_the_right_backend_per_os()
    {
        PackageManagerFactory.Detect(new OsInfo(OsFamily.MacOs, "macos", "", "")).Should().BeOfType<BrewPackageManager>();
        PackageManagerFactory.Detect(new OsInfo(OsFamily.Debian, "ubuntu", "debian", "")).Should().BeOfType<AptPackageManager>();
        PackageManagerFactory.Detect(new OsInfo(OsFamily.Fedora, "fedora", "", "")).Should().BeOfType<DnfPackageManager>();
        PackageManagerFactory.Detect(new OsInfo(OsFamily.Windows, "windows", "", "")).Should().BeOfType<WingetPackageManager>();
        PackageManagerFactory.Detect(new OsInfo(OsFamily.Arch, "arch", "", "")).Should().BeOfType<NullPackageManager>();
        PackageManagerFactory.Detect(new OsInfo(OsFamily.Alpine, "alpine", "", "")).Should().BeOfType<NullPackageManager>();
    }
}
