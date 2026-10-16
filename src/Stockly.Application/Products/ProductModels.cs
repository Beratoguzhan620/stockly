using Stockly.Data.Entities;

namespace Stockly.Application.Products;

public enum StockFilter
{
    All,
    Critical,
    OutOfStock,
}

public enum ActiveFilter
{
    Active,
    Inactive,
    All,
}

public sealed class ProductQuery
{
    public const int DefaultPageSize = 20;

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public StockFilter Stock { get; set; }
    public ActiveFilter Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}

public sealed record ProductRow(
    int Id,
    string Sku,
    string? Barcode,
    string Name,
    string CategoryName,
    string? SupplierName,
    ProductUnit Unit,
    decimal PurchasePrice,
    decimal SalePrice,
    int StockQuantity,
    int CriticalLevel,
    bool IsActive,
    string? ImageUrl)
{
    public bool IsCritical => StockQuantity <= CriticalLevel;
    public decimal StockValue => StockQuantity * PurchasePrice;
}

/// <summary>Everything the add/edit form sends. The opening stock is only used when a product is created.</summary>
public sealed class ProductInput
{
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductUnit Unit { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int CriticalLevel { get; set; }
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public int OpeningStock { get; set; }
    public string? ImageUrl { get; set; }
    public bool RemoveImage { get; set; }
}

public sealed record MovementRow(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    MovementType Type,
    int Quantity,
    int BalanceAfter,
    ProductUnit Unit,
    decimal? UnitCost,
    DateTime OccurredAt,
    string? Note);

public sealed class ProductDetail
{
    public required Product Product { get; init; }
    public required string CategoryName { get; init; }
    public string? SupplierName { get; init; }
    public List<MovementRow> RecentMovements { get; init; } = [];
    public int UnitsInLast30Days { get; init; }
    public int UnitsOutLast30Days { get; init; }

    /// <summary>Days the current stock lasts at the last 30 days' pace; null when nothing went out.</summary>
    public int? DaysOfCover => UnitsOutLast30Days <= 0 ? null : (int)Math.Floor(Product.StockQuantity / (UnitsOutLast30Days / 30.0));
}
