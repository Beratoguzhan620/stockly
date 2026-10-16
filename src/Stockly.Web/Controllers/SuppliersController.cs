using Microsoft.AspNetCore.Mvc;
using Stockly.Application.Common;
using Stockly.Application.Suppliers;
using Stockly.Web.Models;

namespace Stockly.Web.Controllers;

[Route("suppliers")]
public class SuppliersController(SupplierService suppliers) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await suppliers.GetAllAsync());

    [HttpGet("new")]
    public IActionResult Create() => View("Form", new SupplierFormViewModel());

    [HttpPost("new")]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await suppliers.CreateAsync(ToInput(model));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await suppliers.FindAsync(id);
        if (supplier is null)
            return NotFound();

        return View("Form", new SupplierFormViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            Note = supplier.Note,
        });
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await suppliers.UpdateAsync(id, ToInput(model));
        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, bool active)
    {
        var result = await suppliers.SetActiveAsync(id, active);
        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private static SupplierInput ToInput(SupplierFormViewModel model) => new()
    {
        Name = model.Name,
        ContactName = model.ContactName,
        Phone = model.Phone,
        Email = model.Email,
        Address = model.Address,
        Note = model.Note,
    };
}
