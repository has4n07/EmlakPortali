using System.Security.Claims;
using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Models.Entities;
using EmlakPortali.Api.Services;
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
    private readonly IWebHostEnvironment _env;
    private readonly ImageValidationService _imageValidation;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        ImageValidationService imageValidation,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _env = env;
        _imageValidation = imageValidation;
        _logger = logger;
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

    [HttpPost("upload-profile-picture")]
    public async Task<DataResult<object>> UploadProfilePicture(IFormFile file)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return new DataResult<object> { Status = false, Message = "Kullanıcı bulunamadı." };
        }

        // Step 1: Basic file validation
        if (file is null || file.Length == 0)
        {
            return new DataResult<object> { Status = false, Message = "Geçersiz dosya: Dosya boş veya yüklenemedi." };
        }

        // Step 2: File size validation (5MB limit for profile pictures)
        if (file.Length > 5 * 1024 * 1024)
        {
            return new DataResult<object> { Status = false, Message = "Profil fotoğrafı boyutu çok büyük. Maksimum 5MB yükleyebilirsiniz." };
        }

        if (file.Length < 1024)
        {
            return new DataResult<object> { Status = false, Message = "Dosya boyutu çok küçük. Dosya bozuk olabilir." };
        }

        // Step 3: Extension and MIME type validation
        var mimeValidation = _imageValidation.ValidateMimeType(file);
        if (!mimeValidation.IsValid)
        {
            return new DataResult<object> { Status = false, Message = mimeValidation.ErrorMessage! };
        }

        // Step 4: Magic byte validation
        var signatureValidation = _imageValidation.ValidateFileSignature(file);
        if (!signatureValidation.IsValid)
        {
            return new DataResult<object> { Status = false, Message = signatureValidation.ErrorMessage! };
        }

        // Step 5: Image integrity validation
        var integrityValidation = _imageValidation.ValidateImageIntegrity(file);
        if (!integrityValidation.IsValid)
        {
            return new DataResult<object> { Status = false, Message = integrityValidation.ErrorMessage! };
        }

        // Step 6: Sanitize and save the image
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{user.Id}{extension}";
        var folder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "profiles");
        
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var path = Path.Combine(folder, fileName);

        try
        {
            // Delete old profile picture if exists
            if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
            {
                var oldPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), 
                    user.ProfilePictureUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            // Re-encode/sanitize the image (default to jpeg)
            var sanitizationResult = await _imageValidation.SanitizeImageAsync(file, path, "jpeg");
            
            if (!sanitizationResult.IsSuccess)
            {
                _logger.LogWarning("Profile picture sanitization failed for user {UserId}: {Error}", user.Id, sanitizationResult.ErrorMessage);
                return new DataResult<object> { Status = false, Message = sanitizationResult.ErrorMessage! };
            }

            // Update user profile
            var request = ControllerContext.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var fullUrl = $"{baseUrl}/uploads/profiles/{fileName}";
            
            user.ProfilePictureUrl = fullUrl;
            user.Updated = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new DataResult<object> 
            { 
                Status = true, 
                Message = "Profil fotoğrafı başarıyla güncellendi.", 
                Data = new { Url = fullUrl, Width = sanitizationResult.Width, Height = sanitizationResult.Height } 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during profile picture upload for user {UserId}", user.Id);
            
            // Clean up partial file
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            
            return new DataResult<object> { Status = false, Message = "Profil fotoğrafı yüklenirken beklenmeyen bir hata oluştu." };
        }
    }

    [HttpPost("change-password")]
    public async Task<Result> ChangePassword(ChangePasswordDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return new Result { Status = false, Message = "Kullanıcı bulunamadı." };
        }

        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return new Result { Status = false, Message = "Mevcut şifre ve yeni şifre zorunludur." };
        }

        if (dto.NewPassword.Length < 6)
        {
            return new Result { Status = false, Message = "Yeni şifre en az 6 karakter olmalıdır." };
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return new Result { Status = false, Message = "Şifre değiştirilemedi: " + errors };
        }

        return new Result { Status = true, Message = "Şifreniz başarıyla değiştirildi." };
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
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

