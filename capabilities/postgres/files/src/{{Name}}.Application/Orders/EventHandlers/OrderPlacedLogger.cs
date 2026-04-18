using MediatR;
using Microsoft.Extensions.Logging;
using {{Name}}.Domain.Orders;

namespace {{Name}}.Application.Orders.EventHandlers;

/// <summary>
/// Sample domain-event handler — logs that an order was placed. Wires up
/// via the DomainEventsInterceptor, which dispatches after SaveChanges.
/// Replace or delete when you have real handlers (send email, publish
/// integration event, ...).
/// </summary>
internal sealed class OrderPlacedLogger(ILogger<OrderPlacedLogger> log) : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken ct)
    {
        log.LogInformation(
            "Order {OrderId} placed by {Email} (total {Total:C})",
            notification.OrderId, notification.CustomerEmail, notification.Total);
        return Task.CompletedTask;
    }
}
