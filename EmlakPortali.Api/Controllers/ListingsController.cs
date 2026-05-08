using System.Security.Claims;
using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Repositories;
using EmlakPortali.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ListingsController : ControllerBase
{
    private readonly ListingRepository _listingRepository;
    private readonly IWebHostEnvironment _env;
    private readonly ImageValidationService _imageValidation;
    private readonly ILogger<ListingsController> _logger;

    public ListingsController(
        ListingRepository listingRepository, 
        IWebHostEnvironment env,
        ImageValidationService imageValidation,
        ILogger<ListingsController> logger)
    {
        _listingRepository = listingRepository;
        _env = env;
        _imageValidation = imageValidation;
        _logger = logger;
    }

    [HttpGet]
    public async Task<DataResult<object>> PublicList([FromQuery] int take = 50)
    {
        var data = await _listingRepository.GetPublicListAsync(take);
        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

    [HttpGet("search")]
    public async Task<DataResult<object>> Search([FromQuery] ListingSearchQueryDto q)
    {
        var data = await _listingRepository.SearchPublicAsync(q);
        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

    [HttpGet("{id:int}")]
    public async Task<DataResult<object>> PublicDetail(int id)
    {
        var data = await _listingRepository.GetPublicDetailAsync(id);
        if (data is null)
        {
            return new DataResult<object> { Status = false, Message = "İlan bulunamadı." };
        }

        await _listingRepository.IncrementViewCountAsync(id);
        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<DataResult<object>> MyListings([FromQuery] ListingSearchQueryDto q)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new DataResult<object> { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var data = await _listingRepository.SearchMyAsync(userId, q);
        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

    [Authorize]
    [HttpPost("upload-image")]
    public async Task<DataResult<object>> UploadImage(IFormFile file)
    {
        // Step 1: Basic file validation
        if (file is null || file.Length == 0)
        {
            return new DataResult<object> { Status = false, Message = "Geçersiz dosya: Dosya boş veya yüklenemedi." };
        }

        // Step 2: File size validation
        var sizeValidation = _imageValidation.ValidateFileSize(file);
        if (!sizeValidation.IsValid)
        {
            return new DataResult<object> { Status = false, Message = sizeValidation.ErrorMessage! };
        }

        // Step 3: Extension and MIME type validation
        var mimeValidation = _imageValidation.ValidateMimeType(file);
        if (!mimeValidation.IsValid)
        {
            return new DataResult<object> { Status = false, Message = mimeValidation.ErrorMessage! };
        }

        // Step 4: Magic byte validation (file signature)
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
        var fileName = $"{Guid.NewGuid()}{extension}";
        var folder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
        
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var path = Path.Combine(folder, fileName);

        try
        {
            // Re-encode/sanitize the image to remove potential malicious content
            var sanitizationResult = await _imageValidation.SanitizeImageAsync(file, path);
            
            if (!sanitizationResult.IsSuccess)
            {
                _logger.LogWarning("Image sanitization failed for {FileName}: {Error}", file.FileName, sanitizationResult.ErrorMessage);
                return new DataResult<object> { Status = false, Message = sanitizationResult.ErrorMessage! };
            }

            var relativeUrl = $"/uploads/{fileName}";
            var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
            return new DataResult<object> 
            { 
                Status = true, 
                Message = "Görsel başarıyla yüklendi.", 
                Data = new { Url = absoluteUrl, RelativeUrl = relativeUrl, Width = sanitizationResult.Width, Height = sanitizationResult.Height } 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image upload for {FileName}", file.FileName);
            
            // Clean up partial file
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            
            return new DataResult<object> { Status = false, Message = "Görsel yüklenirken beklenmeyen bir hata oluştu." };
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<DataResult<object>> Create(ListingCreateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new DataResult<object> { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var isRealtor = User.IsInRole("Realtor");
        var isAdmin = User.IsInRole("Admin");

        if (!isRealtor && !isAdmin)
        {
            return new DataResult<object> { Status = false, Message = "İlan ekleme yetkisi sadece emlakçı ve admin hesaplarındadır." };
        }

        var entity = await _listingRepository.CreateAsync(userId, dto);
        return new DataResult<object> { Status = true, Message = "Kayıt eklendi. İlan yayında.", Data = new { entity.Id } };
    }

    [Authorize]
    [HttpPost("{id:int}/images")]
    public async Task<DataResult<object>> AddImage(int id, ListingImageCreateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new DataResult<object> { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var listing = await _listingRepository.GetByIdAsync(id);
        if (listing is null)
        {
            return new DataResult<object> { Status = false, Message = "İlan bulunamadı." };
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && listing.OwnerUserId != userId)
        {
            return new DataResult<object> { Status = false, Message = "Bu işlem için yetkiniz yok." };
        }

        var image = await _listingRepository.AddImageAsync(listing, dto);
        return new DataResult<object> { Status = true, Message = "Görsel eklendi.", Data = new { image.Id, image.Url, image.SortOrder } };
    }

    [Authorize]
    [HttpPut("{id:int}/images/{imageId:int}")]
    public async Task<Result> UpdateImage(int id, int imageId, ListingImageUpdateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new Result { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var listing = await _listingRepository.GetByIdAsync(id);
        if (listing is null)
        {
            return new Result { Status = false, Message = "İlan bulunamadı." };
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && listing.OwnerUserId != userId)
        {
            return new Result { Status = false, Message = "Bu işlem için yetkiniz yok." };
        }

        var image = await _listingRepository.GetImageByIdAsync(imageId);
        if (image is null || image.ListingId != id)
        {
            return new Result { Status = false, Message = "Görsel bulunamadı." };
        }

        await _listingRepository.UpdateImageAsync(image, dto);
        return new Result { Status = true, Message = "Görsel güncellendi." };
    }

    [Authorize]
    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<Result> DeleteImage(int id, int imageId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new Result { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var listing = await _listingRepository.GetByIdAsync(id);
        if (listing is null)
        {
            return new Result { Status = false, Message = "İlan bulunamadı." };
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && listing.OwnerUserId != userId)
        {
            return new Result { Status = false, Message = "Bu işlem için yetkiniz yok." };
        }

        var image = await _listingRepository.GetImageByIdAsync(imageId);
        if (image is null || image.ListingId != id)
        {
            return new Result { Status = false, Message = "Görsel bulunamadı." };
        }

        await _listingRepository.DeleteImageAsync(image);
        return new Result { Status = true, Message = "Görsel silindi." };
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<Result> Update(int id, ListingUpdateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new Result { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var entity = await _listingRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return new Result { Status = false, Message = "İlan bulunamadı." };
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && entity.OwnerUserId != userId)
        {
            return new Result { Status = false, Message = "Bu işlem için yetkiniz yok." };
        }

        await _listingRepository.UpdateAsync(entity, dto, isAdmin);
        return new Result { Status = true, Message = "Kayıt güncellendi." };
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<Result> Delete(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new Result { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var entity = await _listingRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return new Result { Status = false, Message = "İlan bulunamadı." };
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && entity.OwnerUserId != userId)
        {
            return new Result { Status = false, Message = "Bu işlem için yetkiniz yok." };
        }

        await _listingRepository.DeleteAsync(entity);
        return new Result { Status = true, Message = "Kayıt silindi." };
    }
}

