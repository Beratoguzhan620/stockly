using Microsoft.AspNetCore.Mvc;
using Stockly.Application.Common;
using Stockly.Application.Products;
using Stockly.Web.Infrastructure;
using Stockly.Web.Models;

namespace Stockly.Web.Controllers;

[Route("products")]
public class ProductsController(ProductService products, Lookups lookups, ImageStorage images) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ProductQuery query) => View(new ProductListViewModel
    {
        Query = query,
        Result = await products.GetPagedAsync(query),
        Categories = await lookups.CategoriesAsync(),
        Suppliers = await lookups.SuppliersAsync(),
    });

    /// <summary>Downloads the products that match the current filters (all pages) as Excel or CSV.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ProductQuery query, [FromQuery] string format = "xlsx")
    {
        var rows = await products.GetAllAsync(query);
        var (content, type, name) = Exports.Build(rows, Exports.ProductColumns, format, "stockly-urunler", "Ürünler");
        return File(content, type, name);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await products.GetDetailAsync(id);
        return detail is null ? NotFound() : View(detail);
    }

    [HttpGet("new")]
    public async Task<IActionResult> Create() => View("Form", await FillAsync(new ProductFormViewModel()));

    [HttpPost("new")]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        var imageError = ImageStorage.Validate(model.Image);
        if (imageError is not null)
            ModelState.AddModelError(nameof(model.Image), imageError);

        if (!ModelState.IsValid)
            return View("Form", await FillAsync(model));

        var imageUrl = model.Image is { Length: > 0 } ? await images.SaveAsync(model.Image) : null;
        var result = await products.CreateAsync(model.ToInput(imageUrl));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", await FillAsync(model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = result.Value });
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await products.FindAsync(id);
        if (product is null)
            return NotFound();

        return View("Form", await FillAsync(new ProductFormViewModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Barcode = product.Barcode,
            Name = product.Name,
            Description = product.Description,
            Unit = product.Unit,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            CriticalLevel = product.CriticalLevel,
            CategoryId = product.CategoryId,
            SupplierId = product.SupplierId,
            CurrentImageUrl = product.ImageUrl,
            CurrentStock = product.StockQuantity,
        }));
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        model.Id = id;
        ModelState.Remove(nameof(model.OpeningStock));

        var imageError = ImageStorage.Validate(model.Image);
        if (imageError is not null)
            ModelState.AddModelError(nameof(model.Image), imageError);

        if (!ModelState.IsValid)
            return View("Form", await FillAsync(model));

        var imageUrl = model.Image is { Length: > 0 } ? await images.SaveAsync(model.Image) : null;
        var result = await products.UpdateAsync(id, model.ToInput(imageUrl));

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", await FillAsync(model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:int}/archive")]
    public Task<IActionResult> Archive(int id) => SetActiveAsync(id, false);

    [HttpPost("{id:int}/restore")]
    public Task<IActionResult> Restore(int id) => SetActiveAsync(id, true);

    private async Task<IActionResult> SetActiveAsync(int id, bool active)
    {
        var result = await products.SetActiveAsync(id, active);
        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task<ProductFormViewModel> FillAsync(ProductFormViewModel model)
    {
        if (model.Id is int id && (model.CurrentImageUrl is null || model.CurrentStock is null))
        {
            var current = await products.FindAsync(id);
            model.CurrentImageUrl ??= current?.ImageUrl;
            model.CurrentStock ??= current?.StockQuantity;
        }

        model.Categories = await lookups.CategoriesAsync();
        model.Suppliers = await lookups.SuppliersAsync(model.SupplierId);
        return model;
    }
}
