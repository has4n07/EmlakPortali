using EmlakPortali.Api.Models;
using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Api.Controllers;

[Authorize(Roles = "Admin")]
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
        var data = await _listingRepository.SearchAdminAsync(q, approved, isActive);
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
        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

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

        await _listingRepository.SetActiveAsync(entity, isActive);

        return new Result { Status = true, Message = "İlan güncellendi." };
    }
}

