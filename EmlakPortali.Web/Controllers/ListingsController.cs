using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Web.Controllers;

public class ListingsController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Details(int id) => View(model: id);
    public IActionResult Create() => View();
    public IActionResult RealtorRequired() => View();
}