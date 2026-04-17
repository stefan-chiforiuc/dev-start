using FluentValidation;
using MediatR;
using {{Name}}.Application.Orders.Contracts;
using {{Name}}.Application.Persistence;
using {{Name}}.Domain.Orders;

namespace {{Name}}.Application.Orders.Commands;

public sealed record PlaceOrder(string CustomerEmail, IReadOnlyCollection<LineItemDto> Lines)
    : IRequest<OrderDto>;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrder>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(li => li.Sku).NotEmpty();
            l.RuleFor(li => li.Quantity).GreaterThan(0);
            l.RuleFor(li => li.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

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
