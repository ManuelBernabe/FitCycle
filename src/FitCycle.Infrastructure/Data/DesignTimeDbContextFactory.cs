using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitCycle.Infrastructure.Data;

/// <summary>
/// Used by EF Core tooling (dotnet ef migrations) at design time.
/// Uses SQLite provider locally since LibSQL is SQLite-compatible.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FitCycleDbContext>
{
    public FitCycleDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FitCycleDbContext>();
        optionsBuilder.UseSqlite("Data Source=fitcycle.db");
        return new FitCycleDbContext(optionsBuilder.Options);
    }
}
