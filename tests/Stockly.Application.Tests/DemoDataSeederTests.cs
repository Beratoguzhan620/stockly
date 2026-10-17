using Microsoft.EntityFrameworkCore;
using Stockly.Data.Entities;
using Stockly.Data.Seeding;

namespace Stockly.Application.Tests;

public sealed class DemoDataSeederTests : IDisposable
{
    private readonly TestDatabase db = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task Seeding_skips_a_database_that_already_has_data()
    {
        // The fixture already added categories.
        await DemoDataSeeder.SeedAsync(db.Context, db.Clock);

        Assert.Empty(db.Fresh().Products);
    }

    [Fact]
    public async Task Every_product_stock_matches_its_movement_history()
    {
        await db.Context.Categories.ExecuteDeleteAsync();
        await db.Context.Suppliers.ExecuteDeleteAsync();
        db.Context.ChangeTracker.Clear();

        await DemoDataSeeder.SeedAsync(db.Context, db.Clock);

        var fresh = db.Fresh();
        var products = await fresh.Products.Include(p => p.Movements).ToListAsync();
        Assert.Equal(30, products.Count);
        Assert.Contains(products, p => p.StockQuantity <= p.CriticalLevel);

        foreach (var product in products)
        {
            var movements = product.Movements.OrderBy(m => m.OccurredAt).ThenBy(m => m.Id).ToList();
            Assert.Equal(product.StockQuantity, movements.Sum(m => m.Quantity));
            Assert.Equal(product.StockQuantity, movements[^1].BalanceAfter);
            Assert.All(movements, m => Assert.True(m.BalanceAfter >= 0));
            Assert.All(movements, m => Assert.True(m.OccurredAt <= db.Clock.GetUtcNow().UtcDateTime.AddDays(1)));
            Assert.True(Stockly.Application.Products.ProductService.IsValidEan13(product.Barcode!));
        }

        Assert.All(products.SelectMany(p => p.Movements).Where(m => m.Type == MovementType.Out), m => Assert.True(m.Quantity < 0));
    }
}
