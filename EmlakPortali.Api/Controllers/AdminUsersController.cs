using EmlakPortali.Api.Models;
using EmlakPortali.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
[ApiController]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<DataResult<object>> List()
    {
        var users = await _userManager.Users
            .OrderByDescending(x => x.Created)
            .Select(x => new { x.Id, x.Email, x.FullName, x.IsActive, x.Created })
            .ToListAsync();

        return new DataResult<object> { Status = true, Message = "OK", Data = users };
    }

    [HttpPut("{userId:guid}/active")]
    public async Task<Result> SetActive(Guid userId, [FromQuery] bool isActive = true)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new Result { Status = false, Message = "Kullanıcı bulunamadı." };
        }

        user.IsActive = isActive;
        user.Updated = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new Result { Status = true, Message = "Kullanıcı güncellendi." };
    }
}

