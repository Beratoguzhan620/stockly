using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockly.Data.Entities;

namespace Stockly.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(60);
        builder.HasIndex(c => c.Name).IsUnique();
    }
}

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(s => s.Name).IsRequired().HasMaxLength(120);
        builder.Property(s => s.ContactName).HasMaxLength(100);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(120);
        builder.Property(s => s.Address).HasMaxLength(250);
        builder.Property(s => s.Note).HasMaxLength(500);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.HasIndex(s => s.Name).IsUnique();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(32);
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Barcode).HasMaxLength(32);
        builder.HasIndex(p => p.Barcode).IsUnique();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(120);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.PurchasePrice).HasPrecision(18, 2);
        builder.Property(p => p.SalePrice).HasPrecision(18, 2);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Products_Prices", "[PurchasePrice] >= 0 AND [SalePrice] >= 0");
            t.HasCheckConstraint("CK_Products_CriticalLevel", "[CriticalLevel] >= 0");
            t.HasCheckConstraint("CK_Products_StockQuantity", "[StockQuantity] >= 0");
        });

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.IsActive, p.StockQuantity });
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.Property(m => m.UnitCost).HasPrecision(18, 2);
        builder.Property(m => m.Note).HasMaxLength(250);

        builder.ToTable(t => t.HasCheckConstraint("CK_StockMovements_BalanceAfter", "[BalanceAfter] >= 0"));

        builder.HasOne(m => m.Product)
            .WithMany(p => p.Movements)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ProductId, m.OccurredAt });
        builder.HasIndex(m => m.OccurredAt);
    }
}
