using Microsoft.EntityFrameworkCore;
using Stockly.Application.Common;
using Stockly.Data;
using Stockly.Data.Entities;

namespace Stockly.Application.Suppliers;

public sealed record SupplierRow(
    int Id,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    bool IsActive,
    int ProductCount,
    int CriticalCount);

public sealed class SupplierInput
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
}

public sealed class SupplierService(AppDbContext context, TimeProvider timeProvider)
{
    public Task<List<SupplierRow>> GetAllAsync(bool includeInactive = true) =>
        context.Suppliers
            .AsNoTracking()
            .Where(s => includeInactive || s.IsActive)
            .OrderByDescending(s => s.IsActive).ThenBy(s => s.Name)
            .Select(s => new SupplierRow(
                s.Id,
                s.Name,
                s.ContactName,
                s.Phone,
                s.Email,
                s.IsActive,
                s.Products.Count(p => p.IsActive),
                s.Products.Count(p => p.IsActive && p.StockQuantity <= p.CriticalLevel)))
            .ToListAsync();

    public Task<Supplier?> FindAsync(int id) => context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Result<int>> CreateAsync(SupplierInput input)
    {
        var check = await ValidateAsync(input, exceptId: null);
        if (!check.Success)
            return Result<int>.Invalid(check.Message);

        var supplier = new Supplier { CreatedAt = timeProvider.GetUtcNow().UtcDateTime };
        Apply(supplier, input);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        return Result<int>.Ok(supplier.Id, $"{supplier.Name} eklendi.");
    }

    public async Task<Result> UpdateAsync(int id, SupplierInput input)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (supplier is null)
            return Result.NotFound("Tedarikçi bulunamadı.");

        var check = await ValidateAsync(input, exceptId: id);
        if (!check.Success)
            return check;

        Apply(supplier, input);
        await context.SaveChangesAsync();
        return Result.Ok("Tedarikçi güncellendi.");
    }

    /// <summary>Suppliers are deactivated rather than deleted, so past products keep their link.</summary>
    public async Task<Result> SetActiveAsync(int id, bool active)
    {
        var updated = await context.Suppliers.Where(s => s.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, active));
        return updated == 0
            ? Result.NotFound("Tedarikçi bulunamadı.")
            : Result.Ok(active ? "Tedarikçi yeniden aktif." : "Tedarikçi pasife alındı.");
    }

    private static void Apply(Supplier supplier, SupplierInput input)
    {
        supplier.Name = input.Name.Trim();
        supplier.ContactName = Clean(input.ContactName);
        supplier.Phone = Clean(input.Phone);
        supplier.Email = Clean(input.Email)?.ToLowerInvariant();
        supplier.Address = Clean(input.Address);
        supplier.Note = Clean(input.Note);
    }

    private async Task<Result> ValidateAsync(SupplierInput input, int? exceptId)
    {
        var name = input.Name?.Trim() ?? string.Empty;
        if (name.Length is < 2 or > 120)
            return Result.Invalid("Tedarikçi adı 2 ile 120 karakter arasında olmalı.");

        var email = Clean(input.Email);
        if (email is not null && (!email.Contains('@') || email.Length > 120))
            return Result.Invalid("Geçerli bir e-posta adresi girin.");

        var existing = await context.Suppliers.Where(s => s.Id != exceptId).Select(s => s.Name).ToListAsync();
        if (existing.Any(n => string.Equals(n.Trim(), name, StringComparison.CurrentCultureIgnoreCase)))
            return Result.Invalid("Bu adla bir tedarikçi zaten var.");

        return Result.Ok();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
