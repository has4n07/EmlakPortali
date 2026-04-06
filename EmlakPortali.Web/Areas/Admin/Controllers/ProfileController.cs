using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ProfileController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

