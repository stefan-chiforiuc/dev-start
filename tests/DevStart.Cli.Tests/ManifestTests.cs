using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class ManifestTests
{
    [Fact]
    public void Roundtrips_through_disk()
    {
        var dir = Directory.CreateTempSubdirectory("devstart-tests-").FullName;
        try
        {
            var original = new Manifest
            {
                Name = "demo",
                Capabilities = ["base", "postgres", "auth"],
                Services = ["api"],
                Deploy = "fly",
            };
            original.Save(dir);

            var loaded = Manifest.Load(dir);
            loaded.Name.Should().Be("demo");
            loaded.Capabilities.Should().Equal("base", "postgres", "auth");
            loaded.Deploy.Should().Be("fly");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Load_throws_when_manifest_missing()
    {
        var dir = Directory.CreateTempSubdirectory("devstart-tests-").FullName;
        try
        {
            var act = () => Manifest.Load(dir);
            act.Should().Throw<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
