namespace {{Name}}.Application.Messaging;

/// <summary>
/// Publishes integration events. Implementations MUST honour the outbox
/// pattern so publishes happen in the same transaction as the aggregate.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class;
}
