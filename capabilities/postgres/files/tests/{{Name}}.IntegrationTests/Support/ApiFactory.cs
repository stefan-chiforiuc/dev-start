using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using {{Name}}.Infrastructure.Persistence;

namespace {{Name}}.IntegrationTests.Support;

public sealed class ApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", postgres.ConnectionString);

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (db.Database.GetMigrations().Any())
            {
                db.Database.Migrate();
            }
            else
            {
                // Pre-migration bootstrap so tests don't require a migration to exist.
                db.Database.EnsureCreated();
            }
        });
    }
}
