using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Stockly.Application.Common;
using Stockly.Application.Products;
using Stockly.Data;
using Stockly.Data.Entities;

namespace Stockly.Application.Stock;

/// <summary>A movement as the user enters it: quantities are always positive; for a count it is the counted quantity.</summary>
public sealed class StockEntry
{
    public int ProductId { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Note { get; set; }

    /// <summary>When the goods moved; defaults to now. Cannot be in the future.</summary>
    public DateTime? OccurredAt { get; set; }
}

public sealed record StockEntryResult(int ProductId, int BalanceAfter, bool IsCritical);

public sealed class MovementQuery
{
    public const int DefaultPageSize = 30;

    public int? ProductId { get; set; }
    public MovementType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}

public sealed class StockService(AppDbContext context, TimeProvider timeProvider)
{
    public const int NoteMaxLength = 250;

    /// <summary>
    /// Records a movement and changes the product's stock in one transaction. The stock update is a single
    /// conditional SQL statement, so two people taking the last item at the same time cannot push it below zero.
    /// </summary>
    public async Task<Result<StockEntryResult>> RecordAsync(StockEntry entry)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var occurredAt = entry.OccurredAt is DateTime at ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : now;

        if (occurredAt > now.AddMinutes(5))
            return Result<StockEntryResult>.Invalid("Gelecek tarihli hareket girilemez.");

        if (!Enum.IsDefined(entry.Type))
            return Result<StockEntryResult>.Invalid("Geçerli bir hareket türü seçin.");

        if (entry.Type == MovementType.Count ? entry.Quantity < 0 : entry.Quantity <= 0)
            return Result<StockEntryResult>.Invalid(entry.Type == MovementType.Count
                ? "Sayılan miktar negatif olamaz."
                : "Miktar en az 1 olmalı.");

        if (entry.UnitCost is < 0)
            return Result<StockEntryResult>.Invalid("Birim maliyet negatif olamaz.");

        var note = string.IsNullOrWhiteSpace(entry.Note) ? null : entry.Note.Trim();
        if (note is { Length: > NoteMaxLength })
            return Result<StockEntryResult>.Invalid($"Not en fazla {NoteMaxLength} karakter olabilir.");

        var product = await context.Products.AsNoTracking()
            .Where(p => p.Id == entry.ProductId)
            .Select(p => new { p.Id, p.Name, p.StockQuantity, p.CriticalLevel, p.IsActive, p.PurchasePrice })
            .FirstOrDefaultAsync();

        if (product is null)
            return Result<StockEntryResult>.NotFound("Ürün bulunamadı.");

        if (!product.IsActive && entry.Type == MovementType.In)
            return Result<StockEntryResult>.Invalid("Arşivlenmiş ürüne giriş yapılamaz. Önce ürünü yeniden etkinleştirin.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        int delta;
        int updated;

        if (entry.Type == MovementType.Count)
        {
            // A count sets the stock to what was counted; it only applies if nobody changed the stock meanwhile.
            delta = entry.Quantity - product.StockQuantity;
            if (delta == 0)
                return Result<StockEntryResult>.Invalid("Sayılan miktar mevcut stokla aynı; kaydedilecek fark yok.");

            updated = await context.Products
                .Where(p => p.Id == product.Id && p.StockQuantity == product.StockQuantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, entry.Quantity).SetProperty(p => p.UpdatedAt, now));

            if (updated == 0)
                return Result<StockEntryResult>.Invalid("Stok bu arada değişti. Sayfayı yenileyip sayımı tekrar girin.");
        }
        else
        {
            delta = entry.Type == MovementType.In ? entry.Quantity : -entry.Quantity;

            updated = await context.Products
                .Where(p => p.Id == product.Id && p.StockQuantity + delta >= 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + delta).SetProperty(p => p.UpdatedAt, now));

            if (updated == 0)
                return Result<StockEntryResult>.Invalid($"Stokta yeterli ürün yok. Mevcut: {product.StockQuantity}.");
        }

        var balance = await context.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync();

        context.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            Type = entry.Type,
            Quantity = delta,
            BalanceAfter = balance,
            UnitCost = entry.Type == MovementType.In ? entry.UnitCost ?? product.PurchasePrice : null,
            Note = note,
            OccurredAt = occurredAt,
            CreatedAt = now,
        });

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        var critical = balance <= product.CriticalLevel;
        var message = entry.Type switch
        {
            MovementType.In => $"{entry.Quantity} birim giriş kaydedildi. Yeni stok: {balance}.",
            MovementType.Out => $"{entry.Quantity} birim çıkış kaydedildi. Yeni stok: {balance}.",
            _ => $"Sayım kaydedildi ({(delta > 0 ? "+" : "")}{delta}). Yeni stok: {balance}.",
        };

        if (critical)
            message += $" Dikkat: {product.Name} kritik seviyede.";

        return Result<StockEntryResult>.Ok(new StockEntryResult(product.Id, balance, critical), message);
    }

    public Task<PagedList<MovementRow>> GetMovementsAsync(MovementQuery query) =>
        Filter(query)
            .OrderByDescending(m => m.OccurredAt).ThenByDescending(m => m.Id)
            .Select(Row)
            .ToPagedListAsync(query.Page, query.PageSize);

    public Task<List<MovementRow>> GetAllMovementsAsync(MovementQuery query) =>
        Filter(query)
            .OrderByDescending(m => m.OccurredAt).ThenByDescending(m => m.Id)
            .Select(Row)
            .ToListAsync();

    private IQueryable<StockMovement> Filter(MovementQuery query)
    {
        var movements = context.StockMovements.AsNoTracking();

        if (query.ProductId is not null)
            movements = movements.Where(m => m.ProductId == query.ProductId);

        if (query.Type is not null)
            movements = movements.Where(m => m.Type == query.Type);

        if (query.From is DateTime from)
            movements = movements.Where(m => m.OccurredAt >= from);

        if (query.To is DateTime to)
        {
            var end = to.Date.AddDays(1);
            movements = movements.Where(m => m.OccurredAt < end);
        }

        return movements;
    }

    internal static readonly Expression<Func<StockMovement, MovementRow>> Row = m => new MovementRow(
        m.Id,
        m.ProductId,
        m.Product.Name,
        m.Product.Sku,
        m.Type,
        m.Quantity,
        m.BalanceAfter,
        m.Product.Unit,
        m.UnitCost,
        m.OccurredAt,
        m.Note);
}
