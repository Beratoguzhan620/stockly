using Microsoft.EntityFrameworkCore;
using Stockly.Data;
using Stockly.Data.Entities;

namespace Stockly.Application.Dashboard;

public sealed class DashboardSummary
{
    public int ActiveProducts { get; init; }
    public int TotalUnits { get; init; }
    public decimal StockValue { get; init; }
    public int CriticalCount { get; init; }
    public int OutOfStockCount { get; init; }
    public int SupplierCount { get; init; }
    public List<CriticalProduct> CriticalProducts { get; init; } = [];
    public List<RecentMovement> RecentMovements { get; init; } = [];
    public List<MonthlyMovement> Monthly { get; init; } = [];
}

public sealed record CriticalProduct(int Id, string Sku, string Name, int StockQuantity, int CriticalLevel, ProductUnit Unit, string? SupplierName);

public sealed record RecentMovement(int ProductId, string ProductName, MovementType Type, int Quantity, int BalanceAfter, ProductUnit Unit, DateTime OccurredAt, string? Note);

public sealed record MonthlyMovement(int Year, int Month, int UnitsIn, int UnitsOut);

public sealed class DashboardService(AppDbContext context, TimeProvider timeProvider)
{
    private const int Months = 6;

    public async Task<DashboardSummary> GetAsync()
    {
        var active = context.Products.AsNoTracking().Where(p => p.IsActive);

        // Prices are summed in memory: the list is small and decimal sums are not portable across providers.
        var stock = await active.Select(p => new { p.StockQuantity, p.PurchasePrice, p.CriticalLevel }).ToListAsync();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(Months - 1));

        var movements = await context.StockMovements
            .AsNoTracking()
            .Where(m => m.OccurredAt >= firstMonth && m.Type != MovementType.Count)
            .GroupBy(m => new { m.OccurredAt.Year, m.OccurredAt.Month, m.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Units = g.Sum(m => m.Quantity) })
            .ToListAsync();

        var monthly = Enumerable.Range(0, Months)
            .Select(i => firstMonth.AddMonths(i))
            .Select(m => new MonthlyMovement(
                m.Year,
                m.Month,
                movements.Where(x => x.Year == m.Year && x.Month == m.Month && x.Type == MovementType.In).Sum(x => x.Units),
                -movements.Where(x => x.Year == m.Year && x.Month == m.Month && x.Type == MovementType.Out).Sum(x => x.Units)))
            .ToList();

        return new DashboardSummary
        {
            ActiveProducts = stock.Count,
            TotalUnits = stock.Sum(p => p.StockQuantity),
            StockValue = stock.Sum(p => p.StockQuantity * p.PurchasePrice),
            CriticalCount = stock.Count(p => p.StockQuantity <= p.CriticalLevel),
            OutOfStockCount = stock.Count(p => p.StockQuantity == 0),
            SupplierCount = await context.Suppliers.CountAsync(s => s.IsActive),
            CriticalProducts = await active
                .Where(p => p.StockQuantity <= p.CriticalLevel)
                .OrderBy(p => p.StockQuantity).ThenBy(p => p.Name)
                .Take(8)
                .Select(p => new CriticalProduct(p.Id, p.Sku, p.Name, p.StockQuantity, p.CriticalLevel, p.Unit, p.Supplier != null ? p.Supplier.Name : null))
                .ToListAsync(),
            RecentMovements = await context.StockMovements
                .AsNoTracking()
                .OrderByDescending(m => m.OccurredAt).ThenByDescending(m => m.Id)
                .Take(8)
                .Select(m => new RecentMovement(m.ProductId, m.Product.Name, m.Type, m.Quantity, m.BalanceAfter, m.Product.Unit, m.OccurredAt, m.Note))
                .ToListAsync(),
            Monthly = monthly,
        };
    }
}
