using Microsoft.EntityFrameworkCore;
using Stockly.Application.Common;
using Stockly.Application.Stock;
using Stockly.Data.Entities;

namespace Stockly.Application.Tests;

public sealed class StockServiceTests : IDisposable
{
    private readonly TestDatabase db = new();
    private readonly StockService service;

    public StockServiceTests()
    {
        service = new StockService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    private Task<Result<StockEntryResult>> Record(int productId, MovementType type, int quantity, DateTime? at = null) =>
        service.RecordAsync(new StockEntry { ProductId = productId, Type = type, Quantity = quantity, OccurredAt = at });

    private async Task<(int Stock, List<StockMovement> Movements)> ReadAsync(int productId)
    {
        var fresh = db.Fresh();
        var stock = await fresh.Products.Where(p => p.Id == productId).Select(p => p.StockQuantity).SingleAsync();
        var movements = await fresh.StockMovements.Where(m => m.ProductId == productId).OrderBy(m => m.Id).ToListAsync();
        return (stock, movements);
    }

    [Fact]
    public async Task Goods_in_raise_the_stock_and_record_the_balance()
    {
        var product = db.AddProduct("KAG-001", stock: 10);

        var result = await Record(product.Id, MovementType.In, 5);

        Assert.True(result.Success, result.Message);
        Assert.Equal(15, result.Value!.BalanceAfter);
        var (stock, movements) = await ReadAsync(product.Id);
        Assert.Equal(15, stock);
        var last = movements[^1];
        Assert.Equal(5, last.Quantity);
        Assert.Equal(15, last.BalanceAfter);
        Assert.Equal(10m, last.UnitCost); // falls back to the purchase price
    }

    [Fact]
    public async Task Goods_out_are_stored_as_a_negative_change()
    {
        var product = db.AddProduct("KAG-001", stock: 10);

        var result = await Record(product.Id, MovementType.Out, 4);

        Assert.True(result.Success, result.Message);
        var (stock, movements) = await ReadAsync(product.Id);
        Assert.Equal(6, stock);
        Assert.Equal(-4, movements[^1].Quantity);
        Assert.Null(movements[^1].UnitCost);
    }

    [Fact]
    public async Task Taking_more_than_the_stock_is_refused_and_changes_nothing()
    {
        var product = db.AddProduct("KAG-001", stock: 3);

        var result = await Record(product.Id, MovementType.Out, 4);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        var (stock, movements) = await ReadAsync(product.Id);
        Assert.Equal(3, stock);
        Assert.Single(movements);
    }

    [Fact]
    public async Task The_last_item_can_go_out()
    {
        var product = db.AddProduct("KAG-001", stock: 3);

        var result = await Record(product.Id, MovementType.Out, 3);

        Assert.True(result.Success, result.Message);
        Assert.Equal(0, (await ReadAsync(product.Id)).Stock);
    }

    [Fact]
    public async Task A_count_sets_the_stock_and_records_the_difference()
    {
        var product = db.AddProduct("KAG-001", stock: 10);

        var result = await Record(product.Id, MovementType.Count, 7);

        Assert.True(result.Success, result.Message);
        var (stock, movements) = await ReadAsync(product.Id);
        Assert.Equal(7, stock);
        Assert.Equal(MovementType.Count, movements[^1].Type);
        Assert.Equal(-3, movements[^1].Quantity);
        Assert.Equal(7, movements[^1].BalanceAfter);
    }

    [Fact]
    public async Task A_count_equal_to_the_stock_is_not_recorded()
    {
        var product = db.AddProduct("KAG-001", stock: 10);

        var result = await Record(product.Id, MovementType.Count, 10);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Single((await ReadAsync(product.Id)).Movements);
    }

    [Theory]
    [InlineData(MovementType.In, 0)]
    [InlineData(MovementType.Out, -1)]
    [InlineData(MovementType.Count, -1)]
    public async Task Invalid_quantities_are_refused(MovementType type, int quantity)
    {
        var product = db.AddProduct("KAG-001", stock: 10);

        var result = await Record(product.Id, type, quantity);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Movements_cannot_be_dated_in_the_future()
    {
        var product = db.AddProduct("KAG-001", stock: 10);

        var result = await Record(product.Id, MovementType.In, 1, db.Clock.GetUtcNow().UtcDateTime.AddDays(1));

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task An_archived_product_cannot_receive_goods()
    {
        var product = db.AddProduct("KAG-001", stock: 10, active: false);

        var result = await Record(product.Id, MovementType.In, 1);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Reaching_the_critical_level_is_reported()
    {
        var product = db.AddProduct("KAG-001", stock: 10, critical: 5);

        var above = await Record(product.Id, MovementType.Out, 4);
        var at = await Record(product.Id, MovementType.Out, 1);

        Assert.False(above.Value!.IsCritical);
        Assert.True(at.Value!.IsCritical);
        Assert.Contains("kritik", at.Message);
    }

    [Fact]
    public async Task An_unknown_product_is_not_found()
    {
        var result = await Record(999, MovementType.In, 1);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task The_movement_list_filters_by_product_type_and_date_newest_first()
    {
        var a = db.AddProduct("KAG-001", stock: 10);
        var b = db.AddProduct("KAG-002", stock: 10);
        await Record(a.Id, MovementType.Out, 1, db.Clock.GetUtcNow().UtcDateTime.AddDays(-3));
        await Record(a.Id, MovementType.Out, 2, db.Clock.GetUtcNow().UtcDateTime.AddDays(-1));
        await Record(b.Id, MovementType.Out, 1);

        var result = await service.GetMovementsAsync(new MovementQuery { ProductId = a.Id, Type = MovementType.Out });
        var recent = await service.GetMovementsAsync(new MovementQuery { From = db.Clock.GetUtcNow().UtcDateTime.AddDays(-2) });

        Assert.Equal(new[] { -2, -1 }, result.Items.Select(m => m.Quantity));
        Assert.Equal(2, recent.TotalCount);
    }
}
