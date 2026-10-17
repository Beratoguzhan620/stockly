using Microsoft.EntityFrameworkCore;
using Stockly.Application.Common;
using Stockly.Application.Products;
using Stockly.Data.Entities;

namespace Stockly.Application.Tests;

public sealed class ProductServiceTests : IDisposable
{
    private readonly TestDatabase db = new();
    private readonly ProductService service;

    public ProductServiceTests()
    {
        service = new ProductService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    private ProductInput Input(string sku = "kag-031", string? barcode = null, int opening = 0) => new()
    {
        Sku = sku,
        Barcode = barcode,
        Name = "  Kraft zarf A4  ",
        Unit = ProductUnit.Pack,
        PurchasePrice = 48m,
        SalePrice = 79.90m,
        CriticalLevel = 5,
        CategoryId = db.Paper.Id,
        SupplierId = db.Supplier.Id,
        OpeningStock = opening,
    };

    [Theory]
    [InlineData("8694410000006", true)]
    [InlineData("8690000000005", true)]
    [InlineData("8690000000001", false)]
    [InlineData("869000000000", false)]
    [InlineData("86900000000A5", false)]
    public void Ean13_check_digit_is_verified(string barcode, bool valid) =>
        Assert.Equal(valid, ProductService.IsValidEan13(barcode));

    [Fact]
    public async Task A_new_product_gets_a_cleaned_sku_and_its_opening_stock_as_a_movement()
    {
        var result = await service.CreateAsync(Input(opening: 12));

        Assert.True(result.Success, result.Message);
        var product = await db.Fresh().Products.Include(p => p.Movements).SingleAsync(p => p.Id == result.Value);
        Assert.Equal("KAG-031", product.Sku);
        Assert.Equal("Kraft zarf A4", product.Name);
        Assert.Equal(12, product.StockQuantity);
        var opening = Assert.Single(product.Movements);
        Assert.Equal(MovementType.In, opening.Type);
        Assert.Equal(12, opening.BalanceAfter);
    }

    [Fact]
    public async Task A_product_without_opening_stock_has_no_movements()
    {
        var result = await service.CreateAsync(Input());

        Assert.Empty(db.Fresh().StockMovements.Where(m => m.ProductId == result.Value));
    }

    [Fact]
    public async Task Sku_and_barcode_must_be_unique()
    {
        await service.CreateAsync(Input(sku: "KAG-031", barcode: "8694410000006"));

        var sameSku = await service.CreateAsync(Input(sku: " kag-031 ", barcode: null));
        var sameBarcode = await service.CreateAsync(Input(sku: "KAG-032", barcode: "869 4410 000006"));

        Assert.Equal(ResultStatus.Invalid, sameSku.Status);
        Assert.Equal(ResultStatus.Invalid, sameBarcode.Status);
    }

    [Fact]
    public async Task A_misread_ean13_barcode_is_refused()
    {
        var result = await service.CreateAsync(Input(barcode: "8690000000001"));

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("kontrol hanesi", result.Message);
    }

    [Theory]
    [InlineData("K", "SKU")]
    [InlineData("KAG 031", "SKU")]
    public async Task Bad_skus_are_refused(string sku, string expected)
    {
        var result = await service.CreateAsync(Input(sku: sku));

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(expected, result.Message);
    }

    [Fact]
    public async Task Negative_prices_and_unknown_categories_are_refused()
    {
        var negative = Input();
        negative.PurchasePrice = -1;
        var noCategory = Input();
        noCategory.CategoryId = 999;

        Assert.Equal(ResultStatus.Invalid, (await service.CreateAsync(negative)).Status);
        Assert.Equal(ResultStatus.Invalid, (await service.CreateAsync(noCategory)).Status);
    }

    [Fact]
    public async Task Editing_a_product_never_changes_its_stock()
    {
        var product = db.AddProduct("KAG-001", stock: 10);
        var input = Input(sku: "KAG-001", opening: 999);

        var result = await service.UpdateAsync(product.Id, input);

        Assert.True(result.Success, result.Message);
        var saved = await db.Fresh().Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal(10, saved.StockQuantity);
        Assert.Equal("Kraft zarf A4", saved.Name);
    }

    [Fact]
    public async Task Archived_products_are_hidden_from_the_default_list()
    {
        db.AddProduct("KAG-001");
        db.AddProduct("KAG-002", active: false);

        var active = await service.GetPagedAsync(new ProductQuery());
        var archived = await service.GetPagedAsync(new ProductQuery { Status = ActiveFilter.Inactive });

        Assert.Equal("KAG-001", Assert.Single(active.Items).Sku);
        Assert.Equal("KAG-002", Assert.Single(archived.Items).Sku);
    }

    [Fact]
    public async Task The_list_filters_by_stock_state_category_and_search()
    {
        db.AddProduct("KAG-001", stock: 10, critical: 3);
        db.AddProduct("KAG-002", stock: 3, critical: 3);
        db.AddProduct("KAG-003", stock: 0, critical: 3);
        db.AddProduct("YAZ-001", stock: 20, critical: 3, category: db.Pens);

        var critical = await service.GetPagedAsync(new ProductQuery { Stock = StockFilter.Critical });
        var outOfStock = await service.GetPagedAsync(new ProductQuery { Stock = StockFilter.OutOfStock });
        var pens = await service.GetPagedAsync(new ProductQuery { CategoryId = db.Pens.Id });
        var search = await service.GetPagedAsync(new ProductQuery { Search = "YAZ" });

        Assert.Equal(new[] { "KAG-002", "KAG-003" }, critical.Items.Select(p => p.Sku).Order());
        Assert.Equal("KAG-003", Assert.Single(outOfStock.Items).Sku);
        Assert.Equal("YAZ-001", Assert.Single(pens.Items).Sku);
        Assert.Equal("YAZ-001", Assert.Single(search.Items).Sku);
    }

    [Fact]
    public async Task Paging_clamps_to_the_last_page()
    {
        for (var i = 1; i <= 5; i++)
            db.AddProduct($"KAG-{i:000}");

        var page = await service.GetPagedAsync(new ProductQuery { PageSize = 2, Page = 99 });

        Assert.Equal(3, page.Page);
        Assert.Equal(3, page.TotalPages);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task Days_of_cover_follow_the_last_30_days_of_sales()
    {
        var product = db.AddProduct("KAG-001", stock: 40);
        var stock = new Stockly.Application.Stock.StockService(db.Context, db.Clock);
        await stock.RecordAsync(new() { ProductId = product.Id, Type = MovementType.Out, Quantity = 10 });

        var detail = await service.GetDetailAsync(product.Id);

        // 10 units out in 30 days → 1 unit every 3 days; 30 left lasts 90 days.
        Assert.Equal(10, detail!.UnitsOutLast30Days);
        Assert.Equal(90, detail.DaysOfCover);
    }
}
