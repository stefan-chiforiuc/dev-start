using MediatR;
using {{Name}}.Application.Orders.Contracts;
using {{Name}}.Application.Persistence;
using {{Name}}.Domain.Orders;

namespace {{Name}}.Application.Orders.Commands;

internal sealed class PlaceOrderHandler(IAppDbContext db, TimeProvider time)
    : IRequestHandler<PlaceOrder, OrderDto>
{
    public async Task<OrderDto> Handle(PlaceOrder request, CancellationToken ct)
    {
        var order = Order.Place(
            request.CustomerEmail,
            request.Lines.Select(l => new LineItem(l.Sku, l.Quantity, l.UnitPrice)),
            time);

        db.Set<Order>().Add(order);
        await db.SaveChangesAsync(ct);

        return Map(order);
    }

    internal static OrderDto Map(Order o) => new(
        o.Id.Value,
        o.CustomerEmail,
        o.PlacedAt,
        o.Lines.Select(l => new LineItemDto(l.Sku, l.Quantity, l.UnitPrice)).ToList(),
        o.Total);
}
