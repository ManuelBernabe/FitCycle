using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace FitCycle.Api.Tests;

/// <summary>
/// Boots the REAL FitCycle API (Program.cs, migrations, seed data, JWT auth) against a
/// throwaway SQLite file so tests exercise the exact same pipeline the PWA hits in
/// production: model binding, EF persistence, and JSON serialization included.
/// </summary>
public class FitCycleApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"fitcycle-e2e-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // DATA_DIR would override the connection string (Railway volume support) — make
        // sure the test host never picks up a real database.
        Environment.SetEnvironmentVariable("DATA_DIR", null);
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { /* best-effort temp cleanup */ }
    }
}
