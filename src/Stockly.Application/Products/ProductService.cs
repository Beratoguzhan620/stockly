using Microsoft.EntityFrameworkCore;
using Stockly.Application.Common;
using Stockly.Application.Stock;
using Stockly.Data;
using Stockly.Data.Entities;

namespace Stockly.Application.Products;

public sealed class ProductService(AppDbContext context, TimeProvider timeProvider)
{
    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public Task<PagedList<ProductRow>> GetPagedAsync(ProductQuery query) =>
        Filter(query)
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Select(Row)
            .ToPagedListAsync(query.Page, query.PageSize);

    /// <summary>Every product that matches the filters, for exports.</summary>
    public Task<List<ProductRow>> GetAllAsync(ProductQuery query) =>
        Filter(query).OrderBy(p => p.Name).ThenBy(p => p.Id).Select(Row).ToListAsync();

    public async Task<ProductDetail?> GetDetailAsync(int id)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return null;

        var since = UtcNow.AddDays(-30);
        var last30 = await context.StockMovements
            .Where(m => m.ProductId == id && m.OccurredAt >= since && m.Type != MovementType.Count)
            .GroupBy(m => m.Type)
            .Select(g => new { Type = g.Key, Units = g.Sum(m => m.Quantity) })
            .ToListAsync();

        return new ProductDetail
        {
            Product = product,
            CategoryName = product.Category.Name,
            SupplierName = product.Supplier?.Name,
            RecentMovements = await context.StockMovements
                .AsNoTracking()
                .Where(m => m.ProductId == id)
                .OrderByDescending(m => m.OccurredAt).ThenByDescending(m => m.Id)
                .Take(15)
                .Select(StockService.Row)
                .ToListAsync(),
            UnitsInLast30Days = last30.Where(x => x.Type == MovementType.In).Sum(x => x.Units),
            UnitsOutLast30Days = -last30.Where(x => x.Type == MovementType.Out).Sum(x => x.Units),
        };
    }

    public Task<Product?> FindAsync(int id) => context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Result<int>> CreateAsync(ProductInput input)
    {
        if (input.OpeningStock < 0)
            return Result<int>.Invalid("Açılış stoğu negatif olamaz.");

        var check = await ValidateAsync(input, exceptId: null);
        if (!check.Success)
            return Result<int>.Invalid(check.Message);

        var now = UtcNow;
        var product = new Product { CreatedAt = now };
        Apply(product, input);
        product.StockQuantity = input.OpeningStock;

        if (input.OpeningStock > 0)
        {
            product.Movements.Add(new StockMovement
            {
                Type = MovementType.In,
                Quantity = input.OpeningStock,
                BalanceAfter = input.OpeningStock,
                UnitCost = input.PurchasePrice,
                Note = "Açılış stoğu",
                OccurredAt = now,
                CreatedAt = now,
            });
        }

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return Result<int>.Ok(product.Id, $"\"{product.Name}\" eklendi.");
    }

    /// <summary>Updates the product card. Stock is never changed here; it only moves through stock movements.</summary>
    public async Task<Result> UpdateAsync(int id, ProductInput input)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return Result.NotFound("Ürün bulunamadı.");

        var check = await ValidateAsync(input, exceptId: id);
        if (!check.Success)
            return check;

        Apply(product, input);
        product.UpdatedAt = UtcNow;
        await context.SaveChangesAsync();

        return Result.Ok("Ürün güncellendi.");
    }

    public async Task<Result> SetActiveAsync(int id, bool active)
    {
        var updated = await context.Products
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, active).SetProperty(p => p.UpdatedAt, UtcNow));

        if (updated == 0)
            return Result.NotFound("Ürün bulunamadı.");

        return Result.Ok(active ? "Ürün yeniden satışta." : "Ürün arşivlendi. Hareket geçmişi korunuyor.");
    }

    public static string NormalizeSku(string? sku) => (sku ?? string.Empty).Trim().ToUpperInvariant();

    public static string? NormalizeBarcode(string? barcode)
    {
        var digits = new string((barcode ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    /// <summary>True when an EAN-13 code's last digit matches its check digit.</summary>
    public static bool IsValidEan13(string barcode)
    {
        if (barcode.Length != 13 || !barcode.All(char.IsAsciiDigit))
            return false;

        var sum = barcode[..12].Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        return (10 - sum % 10) % 10 == barcode[12] - '0';
    }

    private IQueryable<Product> Filter(ProductQuery query)
    {
        var products = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            products = products.Where(p => p.Name.Contains(term) || p.Sku.Contains(term) || (p.Barcode != null && p.Barcode.Contains(term)));
        }

        if (query.CategoryId is not null)
            products = products.Where(p => p.CategoryId == query.CategoryId);

        if (query.SupplierId is not null)
            products = products.Where(p => p.SupplierId == query.SupplierId);

        products = query.Stock switch
        {
            StockFilter.Critical => products.Where(p => p.StockQuantity <= p.CriticalLevel),
            StockFilter.OutOfStock => products.Where(p => p.StockQuantity == 0),
            _ => products,
        };

        return query.Status switch
        {
            ActiveFilter.Active => products.Where(p => p.IsActive),
            ActiveFilter.Inactive => products.Where(p => !p.IsActive),
            _ => products,
        };
    }

    private static readonly System.Linq.Expressions.Expression<Func<Product, ProductRow>> Row = p => new ProductRow(
        p.Id,
        p.Sku,
        p.Barcode,
        p.Name,
        p.Category.Name,
        p.Supplier != null ? p.Supplier.Name : null,
        p.Unit,
        p.PurchasePrice,
        p.SalePrice,
        p.StockQuantity,
        p.CriticalLevel,
        p.IsActive,
        p.ImageUrl);

    private static void Apply(Product product, ProductInput input)
    {
        product.Sku = NormalizeSku(input.Sku);
        product.Barcode = NormalizeBarcode(input.Barcode);
        product.Name = input.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        product.Unit = input.Unit;
        product.PurchasePrice = input.PurchasePrice;
        product.SalePrice = input.SalePrice;
        product.CriticalLevel = input.CriticalLevel;
        product.CategoryId = input.CategoryId;
        product.SupplierId = input.SupplierId;

        if (input.RemoveImage)
            product.ImageUrl = null;
        if (input.ImageUrl is not null)
            product.ImageUrl = input.ImageUrl;
    }

    private async Task<Result> ValidateAsync(ProductInput input, int? exceptId)
    {
        var sku = NormalizeSku(input.Sku);
        if (sku.Length is < 2 or > 32 || !sku.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
            return Result.Invalid("SKU 2-32 karakter olmalı; harf, rakam, tire, alt çizgi ve nokta kullanılabilir.");

        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 120)
            return Result.Invalid("Ürün adı 1 ile 120 karakter arasında olmalı.");

        var barcode = NormalizeBarcode(input.Barcode);
        if (barcode is not null)
        {
            if (barcode.Length > 32 || !barcode.All(char.IsAsciiDigit))
                return Result.Invalid("Barkod yalnızca rakamlardan oluşmalı.");

            if (barcode.Length == 13 && !IsValidEan13(barcode))
                return Result.Invalid("Bu EAN-13 barkodunun kontrol hanesi hatalı. Barkodu tekrar okutun.");
        }

        if (input.PurchasePrice < 0 || input.SalePrice < 0)
            return Result.Invalid("Fiyatlar negatif olamaz.");

        if (input.CriticalLevel < 0)
            return Result.Invalid("Kritik seviye negatif olamaz.");

        if (!Enum.IsDefined(input.Unit))
            return Result.Invalid("Geçerli bir birim seçin.");

        if (!await context.Categories.AnyAsync(c => c.Id == input.CategoryId))
            return Result.Invalid("Geçerli bir kategori seçin.");

        if (input.SupplierId is int supplierId && !await context.Suppliers.AnyAsync(s => s.Id == supplierId))
            return Result.Invalid("Geçerli bir tedarikçi seçin.");

        if (await context.Products.AnyAsync(p => p.Sku == sku && p.Id != exceptId))
            return Result.Invalid($"{sku} SKU'su başka bir üründe kullanılıyor.");

        if (barcode is not null && await context.Products.AnyAsync(p => p.Barcode == barcode && p.Id != exceptId))
            return Result.Invalid("Bu barkod başka bir üründe kullanılıyor.");

        return Result.Ok();
    }
}
