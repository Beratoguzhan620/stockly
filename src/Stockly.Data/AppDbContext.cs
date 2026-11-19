using Microsoft.EntityFrameworkCore;
using Stockly.Data.Entities;

namespace Stockly.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite (the public demo) has no decimal type: EF stores decimals as text there, which cannot be ordered,
        // summed or range-checked. As REAL they can; prices and costs stay well within its precision. SQL Server keeps decimal.
        if (Database.IsSqlite())
            configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
