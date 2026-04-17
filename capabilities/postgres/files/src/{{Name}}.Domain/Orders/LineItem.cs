namespace {{Name}}.Domain.Orders;

public sealed record LineItem(string Sku, int Quantity, decimal UnitPrice)
{
    public decimal Total => UnitPrice * Quantity;
}
