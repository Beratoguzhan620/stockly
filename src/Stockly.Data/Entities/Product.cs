namespace Stockly.Data.Entities;

public class Product
{
    public int Id { get; set; }

    /// <summary>Stock keeping unit, unique per business (e.g. KAG-004).</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>EAN-13 or any barcode printed on the item; optional but unique when set.</summary>
    public string? Barcode { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductUnit Unit { get; set; } = ProductUnit.Piece;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }

    /// <summary>At or below this quantity the product is flagged as critical.</summary>
    public int CriticalLevel { get; set; }

    /// <summary>
    /// Current quantity on hand. Only changed together with a <see cref="StockMovement"/>,
    /// so it always equals the balance of the last movement.
    /// </summary>
    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public List<StockMovement> Movements { get; set; } = [];
}

public enum ProductUnit : byte
{
    Piece = 0,
    Pack = 1,
    Box = 2,
    Set = 3,
    Ream = 4,
}
