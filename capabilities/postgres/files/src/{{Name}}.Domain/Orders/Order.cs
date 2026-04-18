namespace {{Name}}.Domain.Orders;

public sealed class Order : AggregateRoot
{
    // EF.
    private Order() { }

    public OrderId Id { get; private set; }
    public string CustomerEmail { get; private set; } = "";
    public DateTimeOffset PlacedAt { get; private set; }
    private readonly List<LineItem> _lines = [];
    public IReadOnlyCollection<LineItem> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.Total);

    public static Order Place(string customerEmail, IEnumerable<LineItem> lines, TimeProvider time)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);
        var items = lines?.ToList() ?? [];
        if (items.Count == 0)
        {
            throw new DomainException("An order must contain at least one line.");
        }
        if (items.Any(l => l.Quantity <= 0))
        {
            throw new DomainException("Line quantities must be positive.");
        }
        if (items.Any(l => l.UnitPrice < 0))
        {
            throw new DomainException("Line prices must be non-negative.");
        }

        var order = new Order
        {
            Id = OrderId.New(),
            CustomerEmail = customerEmail.Trim(),
            PlacedAt = time.GetUtcNow(),
        };
        order._lines.AddRange(items);
        order.RaiseDomainEvent(new OrderPlaced(order.Id, order.CustomerEmail, order.Total));
        return order;
    }
}

public sealed class DomainException(string message) : Exception(message);
