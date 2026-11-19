using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stockly.Data;

namespace Stockly.Application.Tests;

/// <summary>The public demo's set-up: the app on a SQLite file, created and seeded at start.</summary>
public sealed class SqliteDemoTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"stockly-demo-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(file);
    }

    [Fact]
    public async Task The_demo_database_is_created_and_seeded_once()
    {
        await using var services = new ServiceCollection().AddStocklyData($"Data Source={file}", "Sqlite").BuildServiceProvider();

        await services.PrepareDatabaseAsync(seedDemoData: true);
        // A restart on the same file finds the data and adds nothing.
        await services.PrepareDatabaseAsync(seedDemoData: true);

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(30, await context.Products.CountAsync());
        Assert.True(await context.StockMovements.AnyAsync());
    }

    [Fact]
    public async Task The_native_sqlite_is_the_patched_build()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select sqlite_version()";

        var version = Version.Parse((string)(await command.ExecuteScalarAsync())!);

        // CVE-2025-6965 is fixed in SQLite 3.50.2.
        Assert.True(version >= new Version(3, 50, 2), $"SQLite {version} is loaded.");
    }
}
