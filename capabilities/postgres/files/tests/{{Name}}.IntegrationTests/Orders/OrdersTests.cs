using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using {{Name}}.Application.Orders.Contracts;
using {{Name}}.IntegrationTests.Support;
using Xunit;

namespace {{Name}}.IntegrationTests.Orders;

[Collection(nameof(PostgresCollection))]
public class OrdersTests(PostgresFixture postgres)
{
    private readonly ApiFactory _factory = new(postgres);

    [Fact]
    public async Task Place_order_then_get_returns_same_totals()
    {
        using var client = _factory.CreateClient();

        var body = new
        {
            customerEmail = "dev@example.com",
            lines = new[]
            {
                new { sku = "SKU-001", quantity = 2, unitPrice = 9.99m },
                new { sku = "SKU-002", quantity = 1, unitPrice = 19.50m },
            },
        };

        var post = await client.PostAsJsonAsync("/v1/orders", body);
        post.StatusCode.Should().Be(HttpStatusCode.Created);
        var placed = await post.Content.ReadFromJsonAsync<OrderDto>();
        placed.Should().NotBeNull();
        placed!.Total.Should().Be(2 * 9.99m + 19.50m);

        var get = await client.GetAsync($"/v1/orders/{placed.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await get.Content.ReadFromJsonAsync<OrderDto>();
        fetched!.CustomerEmail.Should().Be("dev@example.com");
        fetched.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task Empty_lines_fail_validation()
    {
        using var client = _factory.CreateClient();

        var body = new { customerEmail = "dev@example.com", lines = Array.Empty<object>() };
        var post = await client.PostAsJsonAsync("/v1/orders", body);
        post.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
