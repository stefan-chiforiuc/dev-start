namespace {{Name}}.Application.Messaging;

/// <summary>
/// Public contract for <see cref="{{Name}}.Domain.Orders.OrderPlaced"/>.
/// Kept separate from the domain event so external consumers bind to a
/// stable shape that doesn't follow internal domain refactors.
/// </summary>
public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    string CustomerEmail,
    decimal Total,
    DateTimeOffset OccurredAt);
