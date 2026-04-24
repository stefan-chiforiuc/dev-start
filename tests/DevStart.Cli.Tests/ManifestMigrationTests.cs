using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class ManifestMigrationTests
{
    [Fact]
    public void V1_manifest_without_stack_or_policies_migrates_to_v2()
    {
        var dir = Directory.CreateTempSubdirectory("devstart-migration-").FullName;
        try
        {
            var path = Path.Join(dir, ".devstart.json");
            File.WriteAllText(path, """
            {
              "schemaVersion": 1,
              "templateVersion": "1.0.0",
              "name": "legacy",
              "capabilities": ["base", "postgres"],
              "services": ["api"],
              "deploy": "none"
            }
            """);

            var loaded = Manifest.Load(dir);
            loaded.SchemaVersion.Should().Be(Manifest.CurrentSchemaVersion);
            loaded.Stack.Should().Be("dotnet-api");
            loaded.Policies.Should().BeEmpty();
            loaded.Capabilities.Should().Equal("base", "postgres");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void V2_manifest_roundtrips_stack_and_policies()
    {
        var dir = Directory.CreateTempSubdirectory("devstart-migration-").FullName;
        try
        {
            var original = new Manifest
            {
                Name = "demo",
                Stack = "typescript-fastify",
                Capabilities = ["ts-base", "ts-postgres"],
                Services = ["api", "web"],
                Deploy = "none",
                Policies = ["default-open-source"],
            };
            original.Save(dir);
            var loaded = Manifest.Load(dir);
            loaded.Stack.Should().Be("typescript-fastify");
            loaded.Policies.Should().Equal("default-open-source");
            loaded.Services.Should().Contain("web");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
