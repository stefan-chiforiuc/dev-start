namespace {{Name}}.Domain.Orders;

public sealed record OrderPlaced(OrderId OrderId, string CustomerEmail, decimal Total) : IDomainEvent;
