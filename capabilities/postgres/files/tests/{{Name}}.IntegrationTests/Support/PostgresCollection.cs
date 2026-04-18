using Xunit;

namespace {{Name}}.IntegrationTests.Support;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
