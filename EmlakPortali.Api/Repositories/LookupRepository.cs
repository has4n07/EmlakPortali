using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Repositories;

public class LookupRepository
{
    private readonly AppDbContext _db;

    public LookupRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<City>> GetCitiesAsync()
    {
        return await _db.Cities
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<List<District>> GetDistrictsByCityAsync(int cityId)
    {
        return await _db.Districts
            .Where(x => x.IsActive && x.CityId == cityId)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
}

