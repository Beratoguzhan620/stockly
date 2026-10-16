using Microsoft.EntityFrameworkCore;
using Stockly.Application.Common;
using Stockly.Data;
using Stockly.Data.Entities;

namespace Stockly.Application.Categories;

public sealed record CategoryRow(int Id, string Name, int ProductCount, int CriticalCount);

public sealed class CategoryService(AppDbContext context, TimeProvider timeProvider)
{
    public const int NameMaxLength = 60;

    public Task<List<CategoryRow>> GetAllAsync() =>
        context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryRow(
                c.Id,
                c.Name,
                c.Products.Count(p => p.IsActive),
                c.Products.Count(p => p.IsActive && p.StockQuantity <= p.CriticalLevel)))
            .ToListAsync();

    public Task<Category?> FindAsync(int id) => context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Result<int>> CreateAsync(string? name)
    {
        var check = await CheckNameAsync(name, exceptId: null);
        if (!check.Success)
            return Result<int>.Invalid(check.Message);

        var category = new Category { Name = Clean(name), CreatedAt = timeProvider.GetUtcNow().UtcDateTime };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return Result<int>.Ok(category.Id, $"\"{category.Name}\" kategorisi eklendi.");
    }

    public async Task<Result> RenameAsync(int id, string? name)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return Result.NotFound("Kategori bulunamadı.");

        var check = await CheckNameAsync(name, exceptId: id);
        if (!check.Success)
            return check;

        category.Name = Clean(name);
        await context.SaveChangesAsync();
        return Result.Ok("Kategori güncellendi.");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return Result.NotFound("Kategori bulunamadı.");

        var productCount = await context.Products.CountAsync(p => p.CategoryId == id);
        if (productCount > 0)
            return Result.Invalid($"Bu kategoride {productCount} ürün var. Önce ürünleri başka bir kategoriye taşıyın.");

        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return Result.Ok($"\"{category.Name}\" kategorisi silindi.");
    }

    /// <summary>Names are compared case-insensitively in Turkish, so "Kâğıt" and "KÂĞIT" count as the same category.</summary>
    private async Task<Result> CheckNameAsync(string? name, int? exceptId)
    {
        var cleaned = Clean(name);

        if (cleaned.Length < 2)
            return Result.Invalid("Kategori adı en az 2 karakter olmalı.");

        if (cleaned.Length > NameMaxLength)
            return Result.Invalid($"Kategori adı en fazla {NameMaxLength} karakter olabilir.");

        var existing = await context.Categories
            .Where(c => c.Id != exceptId)
            .Select(c => c.Name)
            .ToListAsync();

        var key = Key(cleaned);
        if (existing.Any(n => Key(n) == key))
            return Result.Invalid("Bu adla bir kategori zaten var.");

        return Result.Ok();
    }

    private static string Clean(string? name) => string.Join(' ', (name ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Key(string name) => Clean(name).ToLower(Turkish);

    private static readonly System.Globalization.CultureInfo Turkish = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
}
