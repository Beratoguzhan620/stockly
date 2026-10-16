using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stockly.Data;

namespace Stockly.Web.Infrastructure;

/// <summary>Drop-down options shared by several forms.</summary>
public sealed class Lookups(AppDbContext context)
{
    public async Task<List<SelectListItem>> CategoriesAsync() =>
        await context.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();

    /// <summary>Active suppliers, plus the one already linked so editing an old product does not lose it.</summary>
    public async Task<List<SelectListItem>> SuppliersAsync(int? keepId = null) =>
        await context.Suppliers.AsNoTracking()
            .Where(s => s.IsActive || s.Id == keepId)
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem(s.IsActive ? s.Name : s.Name + " (pasif)", s.Id.ToString()))
            .ToListAsync();

    public async Task<List<SelectListItem>> ProductsAsync() =>
        await context.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem($"{p.Name} · {p.Sku} (stok: {p.StockQuantity})", p.Id.ToString()))
            .ToListAsync();
}
