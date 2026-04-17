using MassTransit;
using {{Name}}.Application.Messaging;

namespace {{Name}}.Infrastructure.Messaging;

internal sealed class MassTransitEventPublisher(IPublishEndpoint endpoint) : IEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
        => endpoint.Publish(@event, ct);
}
