using Microsoft.EntityFrameworkCore;
using Stockly.Application.Categories;
using Stockly.Application.Common;
using Stockly.Application.Suppliers;

namespace Stockly.Application.Tests;

public sealed class CategoryAndSupplierTests : IDisposable
{
    private readonly TestDatabase db = new();
    private readonly CategoryService categories;
    private readonly SupplierService suppliers;

    public CategoryAndSupplierTests()
    {
        categories = new CategoryService(db.Context, db.Clock);
        suppliers = new SupplierService(db.Context, db.Clock);
    }

    public void Dispose() => db.Dispose();

    [Theory]
    [InlineData("Kâğıt ürünleri")]
    [InlineData("KÂĞIT ÜRÜNLERİ")]
    [InlineData("  kâğıt   ürünleri ")]
    public async Task Category_names_are_unique_regardless_of_case_and_spaces(string name)
    {
        var result = await categories.CreateAsync(name);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task A_new_category_is_stored_with_tidy_spacing()
    {
        var result = await categories.CreateAsync("  Ambalaj   malzemeleri ");

        Assert.True(result.Success, result.Message);
        Assert.Equal("Ambalaj malzemeleri", (await db.Fresh().Categories.SingleAsync(c => c.Id == result.Value)).Name);
    }

    [Fact]
    public async Task Renaming_a_category_to_its_own_name_is_allowed()
    {
        var result = await categories.RenameAsync(db.Paper.Id, "KÂĞIT ÜRÜNLERİ");

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task A_category_with_products_cannot_be_deleted()
    {
        db.AddProduct("KAG-001");

        var withProducts = await categories.DeleteAsync(db.Paper.Id);
        var empty = await categories.DeleteAsync(db.Pens.Id);

        Assert.Equal(ResultStatus.Invalid, withProducts.Status);
        Assert.True(empty.Success);
        Assert.Equal(1, await db.Fresh().Categories.CountAsync());
    }

    [Fact]
    public async Task The_category_list_counts_active_and_critical_products()
    {
        db.AddProduct("KAG-001", stock: 10, critical: 3);
        db.AddProduct("KAG-002", stock: 2, critical: 3);
        db.AddProduct("KAG-003", stock: 0, critical: 3, active: false);

        var paper = (await categories.GetAllAsync()).Single(c => c.Id == db.Paper.Id);

        Assert.Equal(2, paper.ProductCount);
        Assert.Equal(1, paper.CriticalCount);
    }

    [Fact]
    public async Task Supplier_names_are_unique_and_e_mails_are_checked()
    {
        var duplicate = await suppliers.CreateAsync(new SupplierInput { Name = "marmara kırtasiye" });
        var badEmail = await suppliers.CreateAsync(new SupplierInput { Name = "Deniz Ambalaj", Email = "satis-denizambalaj" });
        var ok = await suppliers.CreateAsync(new SupplierInput { Name = "Deniz Ambalaj", Email = " Satis@DenizAmbalaj.example " });

        Assert.Equal(ResultStatus.Invalid, duplicate.Status);
        Assert.Equal(ResultStatus.Invalid, badEmail.Status);
        Assert.True(ok.Success, ok.Message);
        Assert.Equal("satis@denizambalaj.example", (await db.Fresh().Suppliers.SingleAsync(s => s.Id == ok.Value)).Email);
    }

    [Fact]
    public async Task A_paused_supplier_disappears_from_the_active_list()
    {
        await suppliers.SetActiveAsync(db.Supplier.Id, false);

        Assert.Empty(await suppliers.GetAllAsync(includeInactive: false));
        Assert.Single(await suppliers.GetAllAsync());
    }
}
