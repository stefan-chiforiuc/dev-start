using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Tests in this collection change <see cref="System.IO.Directory.SetCurrentDirectory"/>
/// and must not run in parallel. xUnit serializes tests sharing a collection name.
/// </summary>
[CollectionDefinition("SandboxCwd", DisableParallelization = true)]
public sealed class SandboxCollection;
