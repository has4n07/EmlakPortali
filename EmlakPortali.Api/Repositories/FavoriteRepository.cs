using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Repositories;

public class FavoriteRepository
{
    private readonly AppDbContext _db;

    public FavoriteRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<int>> GetMyFavoriteListingIdsAsync(Guid userId)
    {
        return await _db.FavoriteListings
            .AsNoTracking()
            .Where(x => x.IsActive && x.UserId == userId)
            .OrderByDescending(x => x.Created)
            .Select(x => x.ListingId)
            .ToListAsync();
    }

    public async Task<bool> AddAsync(Guid userId, int listingId)
    {
        var exists = await _db.FavoriteListings.AnyAsync(x => x.UserId == userId && x.ListingId == listingId);
        if (exists) return false;

        _db.FavoriteListings.Add(new FavoriteListing
        {
            UserId = userId,
            ListingId = listingId,
            IsActive = true,
            Created = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveAsync(Guid userId, int listingId)
    {
        var entity = await _db.FavoriteListings.FirstOrDefaultAsync(x => x.UserId == userId && x.ListingId == listingId);
        if (entity is null) return false;

        _db.FavoriteListings.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}

