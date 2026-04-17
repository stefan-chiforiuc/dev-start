namespace {{Name}}.Application.Orders.Contracts;

public sealed record LineItemDto(string Sku, int Quantity, decimal UnitPrice);

public sealed record OrderDto(
    Guid Id,
    string CustomerEmail,
    DateTimeOffset PlacedAt,
    IReadOnlyCollection<LineItemDto> Lines,
    decimal Total);
