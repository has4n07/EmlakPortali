using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EmlakPortali.Web.Controllers;

public class ProjectsController : Controller
{
    private static readonly HttpClient _httpClient = new() { BaseAddress = new Uri("https://localhost:7293") };

    public async Task<IActionResult> Index(string? city = null)
    {
        ViewData["Title"] = "Projeler - Emlak Portali";

        var sampleProjects = new[]
        {
            new { name = "Panorama Istanbul", city = "Istanbul", slug = "panorama-istanbul", totalUnits = 450, deliveryDate = "2026 Q3", minPrice = 4500000m, status = "Satista", imageUrl = "https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?w=600&q=80", cityTag = "istanbul" },
            new { name = "Capital Residence", city = "Ankara", slug = "capital-residence", totalUnits = 280, deliveryDate = "2025 Teslim", minPrice = 2800000m, status = "On Satis", imageUrl = "https://images.unsplash.com/photo-1486325212027-8081e485255e?w=600&q=80", cityTag = "ankara" },
            new { name = "Ege Park Villalari", city = "Izmir", slug = "ege-park", totalUnits = 120, deliveryDate = "2026 Q1", minPrice = 8200000m, status = "Satista", imageUrl = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=600&q=80", cityTag = "izmir" },
            new { name = "Bogazici Towers", city = "Istanbul", slug = "bogazici-towers", totalUnits = 890, deliveryDate = "2027 Q2", minPrice = 12000000m, status = "On Satis", imageUrl = "https://images.unsplash.com/photo-1460472178825-e5240623afd5?w=600&q=80", cityTag = "istanbul" },
            new { name = "Riviera Homes", city = "Antalya", slug = "riviera-homes", totalUnits = 200, deliveryDate = "2025 Teslim", minPrice = 6500000m, status = "Satista", imageUrl = "https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=600&q=80", cityTag = "antalya" },
            new { name = "Green Valley Kagithane", city = "Istanbul", slug = "greenvalley-kagithane", totalUnits = 340, deliveryDate = "2026 Q4", minPrice = 5100000m, status = "On Satis", imageUrl = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=600&q=80", cityTag = "istanbul" }
        };

        try
        {
            var url = "/api/projects";
            if (!string.IsNullOrWhiteSpace(city))
                url += $"?city={city}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var projectsList = data.GetProperty("data").Deserialize<List<JsonElement>>();
                ViewBag.ProjectsList = projectsList;
            }
            else
            {
                ViewBag.ProjectsList = sampleProjects.Where(p => string.IsNullOrWhiteSpace(city) || p.cityTag == city.ToLower()).ToList();
            }
        }
        catch
        {
            ViewBag.ProjectsList = sampleProjects.Where(p => string.IsNullOrWhiteSpace(city) || p.cityTag == city.ToLower()).ToList();
        }

        return View();
    }

    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return RedirectToAction("Index");

        try
        {
            var response = await _httpClient.GetAsync($"/api/projects/{slug}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var project = data.GetProperty("data").Deserialize<JsonElement>();
                ViewData["Title"] = project.GetProperty("name").GetString() + " - Proje Detay";
                ViewBag.Project = project;
                return View();
            }
        }
        catch { }

        // Sample data fallback
        object? projectItem = slug switch
        {
            "panorama-istanbul" => new { name = "Panorama Istanbul", slug = "panorama-istanbul", city = "Istanbul", district = "Beyoglu", description = "Panorama Istanbul, sehirin kalbinde yukselen modern bir yasam projesidir.", imageUrl = "https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?w=600&q=80", totalUnits = 450, deliveryDate = "2026 Q3", minPrice = 4500000m, status = "Satista", roomTypes = "1+1, 2+1, 3+1, 4+1" },
            "capital-residence" => new { name = "Capital Residence", slug = "capital-residence", city = "Ankara", district = "Cankaya", description = "Ankara'nin is merkezine yakin konumda yer alan proje.", imageUrl = "https://images.unsplash.com/photo-1486325212027-8081e485255e?w=600&q=80", totalUnits = 280, deliveryDate = "2025 Teslim", minPrice = 2800000m, status = "On Satis", roomTypes = "2+1, 3+1, 4+1" },
            "ege-park" => new { name = "Ege Park Villalari", slug = "ege-park", city = "Izmir", district = "Cesme", description = "Ege'nin incisi Izmir'de yasam.", imageUrl = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=600&q=80", totalUnits = 120, deliveryDate = "2026 Q1", minPrice = 8200000m, status = "Satista", roomTypes = "3+1, 4+1, 5+1" },
            "bogazici-towers" => new { name = "Bogazici Towers", slug = "bogazici-towers", city = "Istanbul", district = "Sariyer", description = "Istanbul Bogazi'na hakim luks yasam.", imageUrl = "https://images.unsplash.com/photo-1460472178825-e5240623afd5?w=600&q=80", totalUnits = 890, deliveryDate = "2027 Q2", minPrice = 12000000m, status = "On Satis", roomTypes = "2+1, 3+1, 4+1, 5+1" },
            "riviera-homes" => new { name = "Riviera Homes", slug = "riviera-homes", city = "Antalya", district = "Konyaalti", description = "Antalya'nin muhtesem sahil seridi üzerinde.", imageUrl = "https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=600&q=80", totalUnits = 200, deliveryDate = "2025 Teslim", minPrice = 6500000m, status = "Satista", roomTypes = "1+1, 2+1, 3+1" },
            "greenvalley-kagithane" => new { name = "Green Valley Kagithane", slug = "greenvalley-kagithane", city = "Istanbul", district = "Kagithane", description = "Kagithane'nin gelisen bölgesinde yükselen proje.", imageUrl = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=600&q=80", totalUnits = 340, deliveryDate = "2026 Q4", minPrice = 5100000m, status = "On Satis", roomTypes = "1+1, 2+1, 3+1" },
            _ => null
        };

        if (projectItem != null)
        {
            ViewData["Title"] = ((dynamic)projectItem).name + " - Proje Detay";
            ViewBag.Project = JsonSerializer.SerializeToElement((dynamic)projectItem);
            return View();
        }

        ViewData["Title"] = "Proje Bulunamadý";
        return View();
    }
}
