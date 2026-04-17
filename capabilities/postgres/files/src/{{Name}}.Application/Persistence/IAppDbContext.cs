using Microsoft.EntityFrameworkCore;

namespace {{Name}}.Application.Persistence;

/// <summary>
/// Abstraction over the app's EF Core DbContext exposed to the Application
/// layer. Keeps handlers independent of Infrastructure while still allowing
/// EF LINQ queries in-place (no premature repository pattern).
/// </summary>
public interface IAppDbContext
{
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
