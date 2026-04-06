using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    public IActionResult Login()
    {
        return View();
    }
}

