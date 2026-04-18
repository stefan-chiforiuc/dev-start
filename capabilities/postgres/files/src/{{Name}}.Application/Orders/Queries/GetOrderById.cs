using MediatR;
using {{Name}}.Application.Orders.Contracts;

namespace {{Name}}.Application.Orders.Queries;

public sealed record GetOrderById(Guid Id) : IRequest<OrderDto?>;
