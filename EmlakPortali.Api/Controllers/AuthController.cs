using System.Security.Claims;
using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Models.Entities;
using EmlakPortali.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<Result> Register(RegisterRequestDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
        {
            return new Result { Status = false, Message = "Bu e-posta ile kayıt zaten var." };
        }

        var user = new ApplicationUser
        {
            Email = dto.Email.Trim(),
            UserName = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            Created = DateTime.UtcNow
        };

        var create = await _userManager.CreateAsync(user, dto.Password);
        if (!create.Succeeded)
        {
            return new Result
            {
                Status = false,
                Message = string.Join(" | ", create.Errors.Select(e => e.Description))
            };
        }

        await _userManager.AddToRoleAsync(user, "User");

        return new Result { Status = true, Message = "Kayıt başarılı." };
    }

    [HttpPost("login")]
    public async Task<DataResult<AuthResponseDto>> Login(LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return new DataResult<AuthResponseDto> { Status = false, Message = "E-posta veya şifre hatalı." };
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!check.Succeeded)
        {
            return new DataResult<AuthResponseDto> { Status = false, Message = "E-posta veya şifre hatalı." };
        }

        var (token, expiresAtUtc) = await _jwtTokenService.CreateTokenAsync(user);
        return new DataResult<AuthResponseDto>
        {
            Status = true,
            Message = "Giriş başarılı.",
            Data = new AuthResponseDto { AccessToken = token, ExpiresAtUtc = expiresAtUtc }
        };
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<DataResult<object>> Me()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new DataResult<object> { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
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
                user.ProfilePictureUrl,
                user.IsActive,
                Roles = roles
            }
        };
    }
}

