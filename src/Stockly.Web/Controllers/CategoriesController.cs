using Microsoft.AspNetCore.Mvc;
using Stockly.Application.Categories;
using Stockly.Application.Common;
using Stockly.Web.Models;

namespace Stockly.Web.Controllers;

[Route("categories")]
public class CategoriesController(CategoryService categories) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await categories.GetAllAsync());

    [HttpGet("new")]
    public IActionResult Create() => View("Form", new CategoryFormViewModel());

    [HttpPost("new")]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await categories.CreateAsync(model.Name);
        if (!result.Success)
        {
            ModelState.AddModelError(nameof(model.Name), result.Message);
            return View("Form", model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await categories.FindAsync(id);
        if (category is null)
            return NotFound();

        return View("Form", new CategoryFormViewModel { Id = category.Id, Name = category.Name });
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await categories.RenameAsync(id, model.Name);
        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        if (!result.Success)
        {
            ModelState.AddModelError(nameof(model.Name), result.Message);
            return View("Form", model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await categories.DeleteAsync(id);
        if (result.Status == ResultStatus.NotFound)
            return NotFound();

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
