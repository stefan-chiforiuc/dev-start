namespace {{Name}}.Application.Orders.Contracts;

public sealed record LineItemDto(string Sku, int Quantity, decimal UnitPrice);
