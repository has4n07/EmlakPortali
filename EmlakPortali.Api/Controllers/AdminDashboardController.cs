using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Controllers;

[Authorize(Roles = "Admin")]
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
        var totalListings = await _db.Listings.CountAsync();
        var pendingListings = await _db.Listings.CountAsync(x => !x.IsApproved);
        var activeListings = await _db.Listings.CountAsync(x => x.IsApproved && x.IsActive);

        var totalUsers = await _db.Users.CountAsync();
        var activeUsers = await _db.Users.CountAsync(x => x.IsActive);

        return new DataResult<object>
        {
            Status = true,
            Message = "OK",
            Data = new
            {
                totalListings,
                pendingListings,
                activeListings,
                totalUsers,
                activeUsers
            }
        };
    }
}

