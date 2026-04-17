namespace {{Name}}.Domain;

/// <summary>Aggregate roots implement this to expose raised domain events.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
