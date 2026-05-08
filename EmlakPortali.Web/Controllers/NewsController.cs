using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EmlakPortali.Web.Controllers;

public class NewsController : Controller
{
    private static readonly HttpClient _httpClient = new() { BaseAddress = new Uri("https://localhost:7293") };

    public async Task<IActionResult> Index(string? category = null)
    {
        ViewData["Title"] = "Emlak Haberleri - Emlak Portali";
        
        var sampleNews = new[]
        {
            new { title = "2026 Konut Fiyatlari", category = "Piyasa", created = DateTime.Parse("2026-05-07"), readMinutes = 5, imageUrl = "https://images.unsplash.com/photo-1560518883-ce09059eeffa?w=600&q=80", summary = "Turkiye genelinde konut fiyatlari artis gosterdi. Uzmanlar yilin geri kalani icin beklentilerini paylasiyor.", slug = "konut-fiyatlari-2026", hot = true },
            new { title = "Istanbul Kira Artislari", category = "Kiralik", created = DateTime.Parse("2026-05-05"), readMinutes = 4, imageUrl = "https://images.unsplash.com/photo-1486325212027-8081e485255e?w=600&q=80", summary = "Istanbul kira artis hizinin yavasladigi gozlemleniyor.", slug = "kira-artislari-istanbul", hot = false },
            new { title = "Yabanci Yatirimcilar Turkiye Gayrimenkulune Akiyor", category = "Yatirim", created = DateTime.Parse("2026-05-03"), readMinutes = 6, imageUrl = "https://images.unsplash.com/photo-1582407947304-fd86f028f716?w=600&q=80", summary = "Yabanci yatirimcilarin Turkiye gayrimenkul alimlari artti.", slug = "yabanci-yatirimci-turizmi", hot = false },
            new { title = "Enerji Verimli Binalara Vergi Avantaji", category = "Mevzuat", created = DateTime.Parse("2026-05-01"), readMinutes = 3, imageUrl = "https://images.unsplash.com/photo-1518005068251-37900150dfca?w=600&q=80", summary = "Enerji sertifikali binalarda emlak vergisinde indirim uygulanacak.", slug = "enerji-verimli-binalar", hot = false },
            new { title = "Guncel Deprem Yonetmeligi", category = "Mevzuat", created = DateTime.Parse("2026-04-28"), readMinutes = 7, imageUrl = "https://images.unsplash.com/photo-1504711434969-e33886168f5c?w=600&q=80", summary = "Guncel deprem yonetmeligi yeni konut projelerine standartlar getiriyor.", slug = "deprem-yonetmeligi-guncel", hot = false },
            new { title = "Akilli Ev Teknolojileri Standart Oluyor", category = "Teknoloji", created = DateTime.Parse("2026-04-25"), readMinutes = 4, imageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&q=80", summary = "Yeni konut projelerinin cogu artik akilli ev sistemleriyle teslim ediliyor.", slug = "akilli-ev-teknolojileri", hot = false }
        };

        try
        {
            var url = "/api/news";
            if (!string.IsNullOrWhiteSpace(category) && category != "Tümü")
                url += $"?category={category}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var newsList = data.GetProperty("data").Deserialize<List<JsonElement>>();
                ViewBag.NewsList = newsList;
            }
            else
            {
                ViewBag.NewsList = sampleNews.Where(n => string.IsNullOrWhiteSpace(category) || category == "Tümü" || n.category == category).ToList();
            }
        }
        catch
        {
            ViewBag.NewsList = sampleNews.Where(n => string.IsNullOrWhiteSpace(category) || category == "Tümü" || n.category == category).ToList();
        }
        
        return View();
    }

    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return RedirectToAction("Index");

        try
        {
            var response = await _httpClient.GetAsync($"/api/news/{slug}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var news = data.GetProperty("data").Deserialize<JsonElement>();
                ViewData["Title"] = news.GetProperty("title").GetString() + " - Emlak Haberleri";
                ViewBag.News = news;
                return View();
            }
        }
        catch { }

        // Sample data fallback
        object? newsItem = slug switch
        {
            "konut-fiyatlari-2026" => new { title = "2026 Konut Fiyatlari", category = "Piyasa", created = DateTime.Parse("2026-05-07"), readMinutes = 5, imageUrl = "https://images.unsplash.com/photo-1560518883-ce09059eeffa?w=600&q=80", content = "<h4>Konut Fiyatlari Artiyor</h4><p>Turkiye genelinde konut fiyatlari artis gosterdi. Uzmanlar yilin geri kalani icin beklentilerini paylasiyor.</p>" },
            "kira-artislari-istanbul" => new { title = "Istanbul Kira Artislari", category = "Kiralik", created = DateTime.Parse("2026-05-05"), readMinutes = 4, imageUrl = "https://images.unsplash.com/photo-1486325212027-8081e485255e?w=600&q=80", content = "<h4>Kira Artis Hizi Yavasladi</h4><p>Istanbul kira artis hizinin yavasladigi gozlemleniyor.</p>" },
            "yabanci-yatirimci-turizmi" => new { title = "Yabanci Yatirimcilar Turkiye Gayrimenkulune Akiyor", category = "Yatirim", created = DateTime.Parse("2026-05-03"), readMinutes = 6, imageUrl = "https://images.unsplash.com/photo-1582407947304-fd86f028f716?w=600&q=80", content = "<h4>Yabanci Yatirim Artiyor</h4><p>Yabanci yatirimcilarin Turkiye gayrimenkul alimlari artti.</p>" },
            "enerji-verimli-binalar" => new { title = "Enerji Verimli Binalara Vergi Avantaji", category = "Mevzuat", created = DateTime.Parse("2026-05-01"), readMinutes = 3, imageUrl = "https://images.unsplash.com/photo-1518005068251-37900150dfca?w=600&q=80", content = "<h4>Vergi Tesvikleri Geliyor</h4><p>Enerji sertifikali binalarda emlak vergisinde indirim uygulanacak.</p>" },
            "deprem-yonetmeligi-guncel" => new { title = "Guncel Deprem Yonetmeligi", category = "Mevzuat", created = DateTime.Parse("2026-04-28"), readMinutes = 7, imageUrl = "https://images.unsplash.com/photo-1504711434969-e33886168f5c?w=600&q=80", content = "<h4>Yeni Yonetmelik Standartlari</h4><p>Guncel deprem yonetmeligi yeni konut projelerine standartlar getiriyor.</p>" },
            "akilli-ev-teknolojileri" => new { title = "Akilli Ev Teknolojileri Standart Oluyor", category = "Teknoloji", created = DateTime.Parse("2026-04-25"), readMinutes = 4, imageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&q=80", content = "<h4>Teknoloji Entegrasyonu</h4><p>Yeni konut projelerinin cogu artik akilli ev sistemleriyle teslim ediliyor.</p>" },
            _ => null
        };

        if (newsItem != null)
        {
            ViewData["Title"] = ((dynamic)newsItem).title + " - Emlak Haberleri";
            ViewBag.News = JsonSerializer.SerializeToElement((dynamic)newsItem);
            return View();
        }

        ViewData["Title"] = "Haber Bulunamadý";
        return View();
    }
}
