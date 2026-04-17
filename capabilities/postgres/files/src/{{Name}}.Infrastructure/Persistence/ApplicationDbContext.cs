using Microsoft.EntityFrameworkCore;

namespace {{Name}}.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Optimistic concurrency via Postgres xmin.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.AddProperty("xmin", typeof(uint))
                .IsConcurrencyToken = true;
        }

        base.OnModelCreating(modelBuilder);
    }
}
