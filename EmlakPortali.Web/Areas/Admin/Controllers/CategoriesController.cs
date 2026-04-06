using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class CategoriesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
