namespace Stockly.Data.Entities;

/// <summary>One change to a product's stock. Movements are never edited; a mistake is fixed with a new movement.</summary>
public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public MovementType Type { get; set; }

    /// <summary>Signed change: positive for goods in, negative for goods out, either for a count.</summary>
    public int Quantity { get; set; }

    /// <summary>Quantity on hand right after this movement.</summary>
    public int BalanceAfter { get; set; }

    /// <summary>Purchase price per unit for goods in; used for the stock value history.</summary>
    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }

    /// <summary>When the goods actually moved (may be earlier than when it was recorded).</summary>
    public DateTime OccurredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
}

public enum MovementType : byte
{
    /// <summary>Goods received (purchase, return from a customer).</summary>
    In = 1,

    /// <summary>Goods leaving (sale, waste, internal use).</summary>
    Out = 2,

    /// <summary>Stock count: the quantity is set to what was counted.</summary>
    Count = 3,
}
