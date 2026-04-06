using System.Security.Claims;
using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Api.Controllers;

public class ProfileUpdateDto
{
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<DataResult<object>> Me()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return new DataResult<object> { Status = false, Message = "Kullanıcı bulunamadı." };
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new DataResult<object>
        {
            Status = true,
            Message = "OK",
            Data = new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.ProfilePictureUrl,
                user.IsActive,
                Roles = roles
            }
        };
    }

    [HttpPut]
    public async Task<Result> Update(ProfileUpdateDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return new Result { Status = false, Message = "Kullanıcı bulunamadı." };
        }

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        user.ProfilePictureUrl = string.IsNullOrWhiteSpace(dto.ProfilePictureUrl) ? null : dto.ProfilePictureUrl.Trim();
        user.Updated = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new Result { Status = true, Message = "Profil güncellendi." };
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(userId.ToString());
    }
}

