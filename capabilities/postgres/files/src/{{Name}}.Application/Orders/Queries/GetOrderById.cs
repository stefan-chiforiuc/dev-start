using MediatR;
using Microsoft.EntityFrameworkCore;
using {{Name}}.Application.Orders.Commands;
using {{Name}}.Application.Orders.Contracts;
using {{Name}}.Application.Persistence;
using {{Name}}.Domain.Orders;

namespace {{Name}}.Application.Orders.Queries;

public sealed record GetOrderById(Guid Id) : IRequest<OrderDto?>;

internal sealed class GetOrderByIdHandler(IAppDbContext db) : IRequestHandler<GetOrderById, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderById request, CancellationToken ct)
    {
        var order = await db.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == new OrderId(request.Id), ct);
        return order is null ? null : PlaceOrderHandler.Map(order);
    }
}
