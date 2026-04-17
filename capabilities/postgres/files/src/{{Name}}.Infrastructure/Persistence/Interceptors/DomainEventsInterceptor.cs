using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using {{Name}}.Domain;

namespace {{Name}}.Infrastructure.Persistence.Interceptors;

/// <summary>
/// After a successful SaveChanges, dispatches domain events raised by
/// aggregates during this unit of work.
/// </summary>
public sealed class DomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return result;

        var aggregates = eventData.Context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToArray();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToArray();
        foreach (var a in aggregates) a.ClearDomainEvents();

        foreach (var e in events)
        {
            await publisher.Publish(e, cancellationToken);
        }

        return result;
    }
}
