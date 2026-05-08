using System.Security.Claims;
using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Realtor,Admin")]
public class RealtorDashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public RealtorDashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<DataResult<object>> GetStats()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new DataResult<object> { Status = false, Message = "Kullanıcı bilgisi okunamadı." };
        }

        var listings = await _db.Listings
            .Where(x => x.OwnerUserId == userId)
            .ToListAsync();

        var totalListings = listings.Count;
        var totalViews = listings.Sum(x => x.ViewCount);
        
        // Şimdilik pasif olan ilanları "satılmış" veya "kapatılmış" olarak sayıyoruz.
        var soldOrInactiveListings = listings.Count(x => !x.IsActive);

        return new DataResult<object>
        {
            Status = true,
            Message = "OK",
            Data = new
            {
                TotalListings = totalListings,
                TotalViews = totalViews,
                SoldListings = soldOrInactiveListings
            }
        };
    }
}
