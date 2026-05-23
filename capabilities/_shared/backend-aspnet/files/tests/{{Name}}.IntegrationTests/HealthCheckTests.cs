using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace {{Name}}.IntegrationTests;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Healthz_returns_200()
    {
        using var client = _factory.CreateClient();
        var res = await client.GetAsync("/healthz");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
