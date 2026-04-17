using FluentAssertions;
using {{Name}}.Domain.Orders;
using Xunit;

namespace {{Name}}.IntegrationTests.Domain;

/// <summary>
/// Pure domain tests — no DB, no fixtures. Live in the IntegrationTests
/// project for simplicity; they execute in milliseconds regardless.
/// Split into a dedicated DomainTests project once unit tests outgrow
/// this folder.
/// </summary>
public class OrderTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    [Fact]
    public void Place_records_total_and_raises_order_placed_event()
    {
        var order = Order.Place("alice@example.com",
            [new("SKU-001", 2, 9.99m), new("SKU-002", 1, 19.50m)], Clock);

        order.Total.Should().Be(2 * 9.99m + 19.50m);
        order.DomainEvents.Should().ContainSingle(e => e is OrderPlaced);
    }

    [Fact]
    public void Place_rejects_empty_lines()
    {
        var act = () => Order.Place("alice@example.com", [], Clock);
        act.Should().Throw<DomainException>().WithMessage("*at least one line*");
    }

    [Fact]
    public void Place_rejects_non_positive_quantity()
    {
        var act = () => Order.Place("alice@example.com", [new("SKU-001", 0, 9.99m)], Clock);
        act.Should().Throw<DomainException>().WithMessage("*Line quantities*");
    }

    [Fact]
    public void Place_rejects_negative_price()
    {
        var act = () => Order.Place("alice@example.com", [new("SKU-001", 1, -1m)], Clock);
        act.Should().Throw<DomainException>().WithMessage("*Line prices*");
    }

    [Fact]
    public void Place_rejects_empty_email()
    {
        var act = () => Order.Place("", [new("SKU-001", 1, 1m)], Clock);
        act.Should().Throw<ArgumentException>();
    }
}
