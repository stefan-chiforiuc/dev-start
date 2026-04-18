using MediatR;
using {{Name}}.Application.Orders.Contracts;

namespace {{Name}}.Application.Orders.Commands;

public sealed record PlaceOrder(string CustomerEmail, IReadOnlyCollection<LineItemDto> Lines)
    : IRequest<OrderDto>;
