using Microsoft.AspNetCore.Mvc;
using Stockly.Application.Common;
using Stockly.Application.Stock;
using Stockly.Data.Entities;
using Stockly.Web.Infrastructure;
using Stockly.Web.Models;

namespace Stockly.Web.Controllers;

[Route("movements")]
public class MovementsController(StockService stock, Lookups lookups) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] MovementQuery query) => View(new MovementListViewModel
    {
        Query = query,
        Result = await stock.GetMovementsAsync(query),
        Products = await lookups.ProductsAsync(),
    });

    /// <summary>Downloads the movements that match the current filters (all pages) as Excel or CSV.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] MovementQuery query, [FromQuery] string format = "xlsx")
    {
        var rows = await stock.GetAllMovementsAsync(query);
        var (content, type, name) = Exports.Build(rows, Exports.MovementColumns, format, "stockly-hareketler", "Hareketler");
        return File(content, type, name);
    }

    [HttpGet("new")]
    public async Task<IActionResult> Create([FromQuery] int? productId, [FromQuery] MovementType type = MovementType.In) =>
        View(await FillAsync(new StockEntryViewModel { ProductId = productId ?? 0, Type = type }));

    [HttpPost("new")]
    public async Task<IActionResult> Create(StockEntryViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await FillAsync(model));

        var result = await stock.RecordAsync(new StockEntry
        {
            ProductId = model.ProductId,
            Type = model.Type,
            Quantity = model.Quantity,
            UnitCost = model.UnitCost,
            Note = model.Note,
            OccurredAt = model.OccurredAt is DateTime local ? LocalTime.ToUtc(local) : null,
        });

        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await FillAsync(model));
        }

        TempData[result.Value!.IsCritical ? "WarningMessage" : "SuccessMessage"] = result.Message;

        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl!)
            : RedirectToAction(nameof(ProductsController.Detail), "Products", new { id = model.ProductId });
    }

    private async Task<StockEntryViewModel> FillAsync(StockEntryViewModel model)
    {
        model.Products = await lookups.ProductsAsync();
        return model;
    }
}
