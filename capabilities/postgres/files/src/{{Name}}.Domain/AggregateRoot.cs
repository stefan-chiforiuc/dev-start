namespace {{Name}}.Domain;

/// <summary>Base class for aggregate roots. Tracks raised domain events.</summary>
public abstract class AggregateRoot : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
