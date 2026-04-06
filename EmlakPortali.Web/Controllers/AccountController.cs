using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Web.Controllers;

public class AccountController : Controller
{
    public IActionResult Login() => View();
    public IActionResult Register() => View();
    public IActionResult Me() => View();
    public IActionResult MyListings() => View();
    public IActionResult Favorites() => View();
}