using Microsoft.AspNetCore.Mvc;
using Stockly.Application.Dashboard;

namespace Stockly.Web.Controllers;

public class HomeController(DashboardService dashboard) : Controller
{
    public async Task<IActionResult> Index() => View(await dashboard.GetAsync());

    [Route("/not-found")]
    public IActionResult PageNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }

    [Route("/error")]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("Error");
    }
}
