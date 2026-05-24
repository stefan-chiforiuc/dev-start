using MediatR;

namespace {{Name}}.Domain;

/// <summary>Domain events are raised from aggregates and dispatched after SaveChanges.</summary>
public interface IDomainEvent : INotification;
