using Testcontainers.PostgreSql;
using Xunit;

namespace {{Name}}.IntegrationTests.Support;

public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("app")
        .WithUsername("dev")
        .WithPassword("dev")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync() => await Container.StartAsync();
    public async Task DisposeAsync() => await Container.DisposeAsync();
}
