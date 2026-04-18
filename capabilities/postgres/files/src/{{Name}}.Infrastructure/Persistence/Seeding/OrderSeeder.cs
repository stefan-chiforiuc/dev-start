using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using {{Name}}.Domain.Orders;

namespace {{Name}}.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a handful of realistic orders on first boot in Development.
/// Idempotent: exits early if the table already has rows.
/// </summary>
public sealed class OrderSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment env,
    ILogger<OrderSeeder> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!env.IsDevelopment()) return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        if (await db.Set<Order>().AnyAsync(ct))
        {
            log.LogInformation("Orders already seeded; skipping.");
            return;
        }

        var samples = new[]
        {
            Order.Place("alice@example.com",
                [new("SKU-001", 2, 9.99m), new("SKU-002", 1, 19.50m)], time),
            Order.Place("bob@example.com",
                [new("SKU-003", 5, 4.00m)], time),
            Order.Place("carol@example.com",
                [new("SKU-001", 1, 9.99m), new("SKU-004", 3, 12.00m)], time),
        };

        db.Set<Order>().AddRange(samples);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded {Count} sample orders.", samples.Length);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
