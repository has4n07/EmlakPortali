using EmlakPortali.Api.Models;
using EmlakPortali.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Controllers;

public class RoleCreateDto
{
    public string Name { get; set; } = null!;
}

public class RoleAssignDto
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = null!;
}

[Authorize(Roles = "Admin")]
[Route("api/admin/roles")]
[ApiController]
public class AdminRolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminRolesController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<DataResult<object>> List()
    {
        var roles = await _roleManager.Roles
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return new DataResult<object> { Status = true, Message = "OK", Data = roles };
    }

    [HttpPost]
    public async Task<Result> Create(RoleCreateDto dto)
    {
        var name = dto.Name.Trim();
        if (await _roleManager.RoleExistsAsync(name))
        {
            return new Result { Status = false, Message = "Rol zaten var." };
        }

        var res = await _roleManager.CreateAsync(new ApplicationRole { Name = name });
        return new Result
        {
            Status = res.Succeeded,
            Message = res.Succeeded ? "Rol oluşturuldu." : string.Join(" | ", res.Errors.Select(e => e.Description))
        };
    }

    [HttpPost("assign")]
    public async Task<Result> Assign(RoleAssignDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user is null)
        {
            return new Result { Status = false, Message = "Kullanıcı bulunamadı." };
        }

        if (!await _roleManager.RoleExistsAsync(dto.RoleName))
        {
            return new Result { Status = false, Message = "Rol bulunamadı." };
        }

        var res = await _userManager.AddToRoleAsync(user, dto.RoleName);
        return new Result
        {
            Status = res.Succeeded,
            Message = res.Succeeded ? "Rol atandı." : string.Join(" | ", res.Errors.Select(e => e.Description))
        };
    }
}

