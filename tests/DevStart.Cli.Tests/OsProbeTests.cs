using DevStart.Install;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class OsProbeTests
{
    [Theory]
    [InlineData("ubuntu", "debian", OsFamily.Debian)]
    [InlineData("debian", "", OsFamily.Debian)]
    [InlineData("linuxmint", "ubuntu debian", OsFamily.Debian)]
    [InlineData("fedora", "", OsFamily.Fedora)]
    [InlineData("rhel", "fedora", OsFamily.Fedora)]
    [InlineData("rocky", "rhel centos fedora", OsFamily.Fedora)]
    [InlineData("almalinux", "rhel centos fedora", OsFamily.Fedora)]
    [InlineData("arch", "", OsFamily.Arch)]
    [InlineData("manjaro", "arch", OsFamily.Arch)]
    [InlineData("alpine", "", OsFamily.Alpine)]
    [InlineData("opensuse-leap", "suse", OsFamily.OpenSuse)]
    [InlineData("nixos", "", OsFamily.OtherLinux)]
    public void Classifies_known_distros_from_os_release(string id, string idLike, OsFamily expected)
    {
        var content = $"ID={id}\nID_LIKE=\"{idLike}\"\nPRETTY_NAME=\"Test\"\n";
        var info = OsProbe.Parse(content);
        info.Family.Should().Be(expected);
        info.Id.Should().Be(id);
    }

    [Fact]
    public void Handles_quoted_and_unquoted_values()
    {
        var info = OsProbe.Parse("ID=\"ubuntu\"\nID_LIKE=debian\n");
        info.Family.Should().Be(OsFamily.Debian);
        info.Id.Should().Be("ubuntu");
        info.IdLike.Should().Be("debian");
    }

    [Fact]
    public void Defaults_to_other_linux_for_empty_content()
    {
        var info = OsProbe.Parse("");
        info.Family.Should().Be(OsFamily.OtherLinux);
    }
}
