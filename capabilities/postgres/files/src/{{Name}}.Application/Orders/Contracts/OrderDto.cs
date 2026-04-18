namespace {{Name}}.Application.Orders.Contracts;

public sealed record OrderDto(
    Guid Id,
    string CustomerEmail,
    DateTimeOffset PlacedAt,
    IReadOnlyCollection<LineItemDto> Lines,
    decimal Total);
