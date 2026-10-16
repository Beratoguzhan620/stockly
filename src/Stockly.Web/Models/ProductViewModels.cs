using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stockly.Application.Common;
using Stockly.Application.Products;
using Stockly.Data.Entities;

namespace Stockly.Web.Models;

public class ProductFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "SKU")]
    [Required(ErrorMessage = "SKU girin.")]
    [StringLength(32, MinimumLength = 2, ErrorMessage = "SKU 2 ile 32 karakter arasında olmalı.")]
    public string Sku { get; set; } = string.Empty;

    [Display(Name = "Barkod")]
    [StringLength(32, ErrorMessage = "Barkod en fazla 32 karakter olabilir.")]
    public string? Barcode { get; set; }

    [Display(Name = "Ürün adı")]
    [Required(ErrorMessage = "Ürün adını girin.")]
    [StringLength(120, ErrorMessage = "Ürün adı en fazla 120 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Birim")]
    public ProductUnit Unit { get; set; } = ProductUnit.Piece;

    [Display(Name = "Alış fiyatı (₺)")]
    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Geçerli bir fiyat girin.")]
    public decimal PurchasePrice { get; set; }

    [Display(Name = "Satış fiyatı (₺)")]
    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Geçerli bir fiyat girin.")]
    public decimal SalePrice { get; set; }

    [Display(Name = "Kritik seviye")]
    [Range(0, 1_000_000, ErrorMessage = "Kritik seviye 0 veya daha büyük olmalı.")]
    public int CriticalLevel { get; set; } = 5;

    [Display(Name = "Kategori")]
    [Range(1, int.MaxValue, ErrorMessage = "Bir kategori seçin.")]
    public int CategoryId { get; set; }

    [Display(Name = "Tedarikçi")]
    public int? SupplierId { get; set; }

    [Display(Name = "Açılış stoğu")]
    [Range(0, 1_000_000, ErrorMessage = "Açılış stoğu 0 veya daha büyük olmalı.")]
    public int OpeningStock { get; set; }

    [Display(Name = "Görsel")]
    public IFormFile? Image { get; set; }

    [Display(Name = "Görseli kaldır")]
    public bool RemoveImage { get; set; }

    public string? CurrentImageUrl { get; set; }
    public int? CurrentStock { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = [];
    public IEnumerable<SelectListItem> Suppliers { get; set; } = [];

    public ProductInput ToInput(string? imageUrl) => new()
    {
        Sku = Sku,
        Barcode = Barcode,
        Name = Name,
        Description = Description,
        Unit = Unit,
        PurchasePrice = PurchasePrice,
        SalePrice = SalePrice,
        CriticalLevel = CriticalLevel,
        CategoryId = CategoryId,
        SupplierId = SupplierId,
        OpeningStock = OpeningStock,
        ImageUrl = imageUrl,
        RemoveImage = RemoveImage,
    };
}

public class ProductListViewModel
{
    public required PagedList<ProductRow> Result { get; init; }
    public required ProductQuery Query { get; init; }
    public IEnumerable<SelectListItem> Categories { get; init; } = [];
    public IEnumerable<SelectListItem> Suppliers { get; init; } = [];

    public PagerViewModel Pager => new(Result.Page, Result.PageSize, Result.TotalCount, Result.TotalPages);
}

public class StockEntryViewModel
{
    [Display(Name = "Ürün")]
    [Range(1, int.MaxValue, ErrorMessage = "Bir ürün seçin.")]
    public int ProductId { get; set; }

    [Display(Name = "Hareket türü")]
    public MovementType Type { get; set; } = MovementType.In;

    [Display(Name = "Miktar")]
    [Range(0, 1_000_000, ErrorMessage = "Geçerli bir miktar girin.")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Birim maliyet (₺)")]
    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Geçerli bir tutar girin.")]
    public decimal? UnitCost { get; set; }

    [Display(Name = "Not")]
    [StringLength(250, ErrorMessage = "Not en fazla 250 karakter olabilir.")]
    public string? Note { get; set; }

    [Display(Name = "Tarih")]
    [DataType(DataType.DateTime)]
    public DateTime? OccurredAt { get; set; }

    /// <summary>Where to go back after saving (the product page or the movements page).</summary>
    public string? ReturnUrl { get; set; }

    public IEnumerable<SelectListItem> Products { get; set; } = [];
}

public class MovementListViewModel
{
    public required PagedList<MovementRow> Result { get; init; }
    public required Stockly.Application.Stock.MovementQuery Query { get; init; }
    public IEnumerable<SelectListItem> Products { get; init; } = [];

    public PagerViewModel Pager => new(Result.Page, Result.PageSize, Result.TotalCount, Result.TotalPages);
}

public class SupplierFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Firma adı")]
    [Required(ErrorMessage = "Firma adını girin.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Firma adı 2 ile 120 karakter arasında olmalı.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Yetkili kişi")]
    [StringLength(100)]
    public string? ContactName { get; set; }

    [Display(Name = "Telefon")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Display(Name = "E-posta")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(120)]
    public string? Email { get; set; }

    [Display(Name = "Adres")]
    [StringLength(250)]
    public string? Address { get; set; }

    [Display(Name = "Not")]
    [StringLength(500)]
    public string? Note { get; set; }
}
