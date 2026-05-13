using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmlakPortali.Api.Controllers;

[Authorize(Roles = "Admin,Realtor")]
[Route("api/admin/dashboard")]
[ApiController]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminDashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<DataResult<object>> Stats()
    {
        var isAdmin = User.IsInRole("Admin");
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) 
        {
            return new DataResult<object> { Status = false, Message = "Kullanıcı bilgisi alınamadı." };
        }

        var query = _db.Listings.AsQueryable();
        if (!isAdmin)
        {
            query = query.Where(x => x.OwnerUserId == userId);
        }

        var totalListings = await query.CountAsync();
        var activeListings = await query.CountAsync(x => x.IsApproved && x.IsActive);

        var msgQuery = _db.ContactMessages.AsQueryable().Where(x => x.ReceiverId == userId);
        var newMessages = await msgQuery.CountAsync(x => !x.IsRead);

        int? totalUsers = null;
        int? activeUsers = null;

        if (isAdmin)
        {
            totalUsers = await _db.Users.CountAsync();
            activeUsers = await _db.Users.CountAsync(x => x.IsActive);
        }

        return new DataResult<object>
        {
            Status = true,
            Message = "OK",
            Data = new
            {
                totalListings,
                newMessages,
                activeListings,
                totalUsers,
                activeUsers
            }
        };
    }
}

