using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Stockly.Data;
using Stockly.Data.Entities;

namespace Stockly.Application.Tests;

/// <summary>
/// A fresh in-memory SQLite database per test with a small catalogue. SQLite runs real SQL, so
/// transactions, unique indexes, check constraints and ExecuteUpdate behave like they do on SQL Server.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection connection;

    public TestDatabase()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        Context = CreateContext();
        Context.Database.EnsureCreated();

        var now = Clock.GetUtcNow().UtcDateTime;
        Paper = new Category { Name = "Kâğıt ürünleri", CreatedAt = now };
        Pens = new Category { Name = "Yazım gereçleri", CreatedAt = now };
        Supplier = new Supplier { Name = "Marmara Kırtasiye", CreatedAt = now };
        Context.AddRange(Paper, Pens, Supplier);
        Context.SaveChanges();
    }

    public AppDbContext Context { get; }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));

    public Category Paper { get; }
    public Category Pens { get; }
    public Supplier Supplier { get; }

    /// <summary>A separate context on the same database, to read what was really saved.</summary>
    public AppDbContext Fresh() => CreateContext();

    public Product AddProduct(string sku, int stock = 10, int critical = 3, bool active = true, Category? category = null)
    {
        var now = Clock.GetUtcNow().UtcDateTime;
        var product = new Product
        {
            Sku = sku,
            Name = $"Ürün {sku}",
            PurchasePrice = 10m,
            SalePrice = 15m,
            CriticalLevel = critical,
            StockQuantity = stock,
            IsActive = active,
            CategoryId = (category ?? Paper).Id,
            CreatedAt = now,
        };

        if (stock > 0)
            product.Movements.Add(new StockMovement { Type = MovementType.In, Quantity = stock, BalanceAfter = stock, OccurredAt = now.AddDays(-30), CreatedAt = now });

        Context.Products.Add(product);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return product;
    }

    private AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    public void Dispose()
    {
        Context.Dispose();
        connection.Dispose();
    }
}
