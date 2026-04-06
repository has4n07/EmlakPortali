using System.Security.Claims;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class FavoritesController : ControllerBase
{
    private readonly FavoriteRepository _favoriteRepository;

    public FavoritesController(FavoriteRepository favoriteRepository)
    {
        _favoriteRepository = favoriteRepository;
    }

    [HttpGet]
    public async Task<DataResult<object>> MyFavorites()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new DataResult<object> { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var ids = await _favoriteRepository.GetMyFavoriteListingIdsAsync(userId);
        return new DataResult<object> { Status = true, Message = "OK", Data = ids };
    }

    [HttpPost("{listingId:int}")]
    public async Task<Result> Add(int listingId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new Result { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var added = await _favoriteRepository.AddAsync(userId, listingId);
        return new Result { Status = added, Message = added ? "Favorilere eklendi." : "Zaten favorilerde." };
    }

    [HttpDelete("{listingId:int}")]
    public async Task<Result> Remove(int listingId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new Result { Status = false, Message = "Token kullanıcı bilgisi okunamadı." };
        }

        var removed = await _favoriteRepository.RemoveAsync(userId, listingId);
        return new Result { Status = removed, Message = removed ? "Favorilerden kaldırıldı." : "Favoride bulunamadı." };
    }
}

