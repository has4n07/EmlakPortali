using EmlakPortali.Api.Models;
using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmlakPortali.Api.Controllers;

[Authorize(Roles = "Admin,Realtor")]
[Route("api/admin/listings")]
[ApiController]
public class AdminListingsController : ControllerBase
{
    private readonly ListingRepository _listingRepository;

    public AdminListingsController(ListingRepository listingRepository)
    {
        _listingRepository = listingRepository;
    }

    [HttpGet]
    public async Task<DataResult<object>> Search([FromQuery] ListingSearchQueryDto q, [FromQuery] bool? approved = null, [FromQuery] bool? isActive = null)
    {
        var isAdmin = User.IsInRole("Admin");
        var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        Guid? ownerUserId = null;
        if (!isAdmin && Guid.TryParse(userIdStr, out var userId))
        {
            ownerUserId = userId;
        }

        var data = await _listingRepository.SearchAdminAsync(q, approved, isActive, ownerUserId);
        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

    [HttpGet("{id:int}")]
    public async Task<DataResult<object>> GetDetail(int id)
    {
        var data = await _listingRepository.GetAdminDetailAsync(id);
        if (data is null)
        {
            return new DataResult<object> { Status = false, Message = "İlan bulunamadı." };
        }
        
        var isAdmin = User.IsInRole("Admin");
        var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!isAdmin && Guid.TryParse(userIdStr, out var userId))
        {
            var props = data.GetType().GetProperty("OwnerUserId");
            if (props != null)
            {
                var ownerUserId = (Guid)props.GetValue(data)!;
                if (ownerUserId != userId)
                {
                    return new DataResult<object> { Status = false, Message = "Bu ilanı görme yetkiniz yok." };
                }
            }
        }

        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/approve")]
    public async Task<Result> Approve(int id, [FromQuery] bool approved = true)
    {
        var entity = await _listingRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return new Result { Status = false, Message = "İlan bulunamadı." };
        }

        await _listingRepository.ApproveAsync(entity, approved);
        return new Result { Status = true, Message = approved ? "İlan onaylandı." : "İlan onayı kaldırıldı." };
    }

    [HttpPut("{id:int}/active")]
    public async Task<Result> SetActive(int id, [FromQuery] bool isActive = true)
    {
        var entity = await _listingRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return new Result { Status = false, Message = "İlan bulunamadı." };
        }

        var isAdmin = User.IsInRole("Admin");
        var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!isAdmin && Guid.TryParse(userIdStr, out var userId))
        {
            if (entity.OwnerUserId != userId)
            {
                return new Result { Status = false, Message = "Bu ilanı düzenleme yetkiniz yok." };
            }
        }

        await _listingRepository.SetActiveAsync(entity, isActive);

        return new Result { Status = true, Message = "İlan güncellendi." };
    }
}

