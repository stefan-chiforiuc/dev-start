using MediatR;
using {{Name}}.Application.Messaging;
using {{Name}}.Domain.Orders;

namespace {{Name}}.Application.Orders.EventHandlers;

/// <summary>
/// Bridges the internal <see cref="OrderPlaced"/> domain event to the
/// public <see cref="OrderPlacedIntegrationEvent"/>. Publishes via the
/// outbox (see <c>MassTransitEventPublisher</c>), so delivery happens in
/// the same transaction as the order aggregate's SaveChanges.
/// </summary>
internal sealed class PublishOrderPlacedIntegrationEvent(
    IEventPublisher publisher,
    TimeProvider time)
    : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken ct)
        => publisher.PublishAsync(
            new OrderPlacedIntegrationEvent(
                notification.OrderId.Value,
                notification.CustomerEmail,
                notification.Total,
                time.GetUtcNow()),
            ct);
}
