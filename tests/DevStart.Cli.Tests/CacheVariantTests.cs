using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// End-to-end coverage for the cache engine swap: cache-memory ships an
/// in-process IMemoryCache wiring that satisfies the same ITypedCache
/// contract as the Redis-backed cache, and conflicts with it.
/// </summary>
[Collection("SandboxCwd")]
public sealed class CacheVariantTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _priorCwd;

    public CacheVariantTests()
    {
        _sandbox = Directory.CreateTempSubdirectory("devstart-cache-variant-").FullName;
        _priorCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_sandbox);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_priorCwd);
        try { Directory.Delete(_sandbox, recursive: true); } catch { /* best-effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Cache_memory_wires_in_process_implementation_into_dotnet_project()
    {
        // Pick the memory variant via the family map; assert no Redis
        // wiring leaks in.
        var planner = new Planner(
            name: "mem-app",
            multiService: false,
            capabilities: ["cache"],
            deployTarget: "none",
            includeClaude: false,
            familyChoices: new Dictionary<string, string> { ["cache"] = "memory" });

        await planner.RunAsync();
        var root = Path.Combine(_sandbox, "mem-app");

        File.Exists(Path.Combine(root, "src/MemApp.Infrastructure/Caching/MemoryTypedCache.cs"))
            .Should().BeTrue();

        var module = File.ReadAllText(Path.Combine(root, "src/MemApp.Infrastructure/CacheModule.cs"));
        module.Should().Contain("AddMemoryCache");
        module.Should().NotContain("AddStackExchangeRedisCache",
            because: "memory variant must not pull in Redis wiring");

        var infraDi = File.ReadAllText(Path.Combine(root, "src/MemApp.Infrastructure/DependencyInjection.cs"));
        infraDi.Should().Contain("services.AddCache(config);");

        // Manifest records the concrete variant so doctor/add/upgrade see it.
        var manifest = Manifest.Load(root);
        manifest.Capabilities.Should().Contain("cache-memory");
        manifest.Capabilities.Should().NotContain("cache");
    }

    [Fact]
    public void Cache_and_cache_memory_conflict_in_capability_metadata()
    {
        var redis = Capability.LoadEmbedded("cache");
        var memory = Capability.LoadEmbedded("cache-memory");

        redis.ConflictsWith.Should().Contain("cache-memory");
        memory.ConflictsWith.Should().Contain("cache");
    }

    [Fact]
    public void Ts_cache_memory_is_registered_for_typescript_stack()
    {
        var cap = Capability.LoadEmbedded("ts-cache-memory");
        cap.Stacks.Should().Contain("typescript-fastify");
        cap.Family.Should().Be("cache");
        cap.Framework.Should().Be("memory");
        cap.Provides.Should().Contain("ts-cache");
    }
}
