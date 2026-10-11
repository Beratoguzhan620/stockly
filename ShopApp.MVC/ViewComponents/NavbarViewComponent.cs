using Microsoft.AspNetCore.Mvc;
using ShopApp.MVC.Data;

namespace ShopApp.MVC.ViewComponents;

public class NavbarViewComponent(AppDbContext context) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var categories = context.Categories.ToList();
        return View(categories);
    }
}
