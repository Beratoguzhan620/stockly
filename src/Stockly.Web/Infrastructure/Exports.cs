using Stockly.Application.Export;
using Stockly.Application.Products;

namespace Stockly.Web.Infrastructure;

/// <summary>Column sets and file responses for the product and movement exports.</summary>
public static class Exports
{
    public static readonly IReadOnlyList<ExportColumn<ProductRow>> ProductColumns =
    [
        new("SKU", p => p.Sku),
        new("Barkod", p => p.Barcode),
        new("Ürün", p => p.Name),
        new("Kategori", p => p.CategoryName),
        new("Tedarikçi", p => p.SupplierName),
        new("Birim", p => Display.Unit(p.Unit)),
        new("Stok", p => p.StockQuantity, ExportFormat.Integer),
        new("Kritik seviye", p => p.CriticalLevel, ExportFormat.Integer),
        new("Alış fiyatı", p => p.PurchasePrice, ExportFormat.Money),
        new("Satış fiyatı", p => p.SalePrice, ExportFormat.Money),
        new("Stok değeri", p => p.StockValue, ExportFormat.Money),
        new("Durum", p => !p.IsActive ? "Arşivde" : p.StockQuantity == 0 ? "Tükendi" : p.IsCritical ? "Kritik" : "Normal"),
    ];

    public static readonly IReadOnlyList<ExportColumn<MovementRow>> MovementColumns =
    [
        new("Tarih", m => LocalTime.FromUtc(m.OccurredAt), ExportFormat.DateTime),
        new("SKU", m => m.Sku),
        new("Ürün", m => m.ProductName),
        new("Tür", m => Display.Movement(m.Type)),
        new("Miktar", m => m.Quantity, ExportFormat.Integer),
        new("Kalan", m => m.BalanceAfter, ExportFormat.Integer),
        new("Birim", m => Display.Unit(m.Unit)),
        new("Birim maliyet", m => m.UnitCost, ExportFormat.Money),
        new("Not", m => m.Note),
    ];

    public const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string CsvType = "text/csv; charset=utf-8";

    public static (byte[] Content, string ContentType, string FileName) Build<T>(
        IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string format, string baseName, string sheetName)
    {
        var stamp = LocalTime.FromUtc(DateTime.UtcNow).ToString("yyyy-MM-dd");
        return string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
            ? (TableExport.ToCsv(rows, columns), CsvType, $"{baseName}-{stamp}.csv")
            : (TableExport.ToXlsx(rows, columns, sheetName), XlsxType, $"{baseName}-{stamp}.xlsx");
    }
}
