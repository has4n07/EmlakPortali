using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Repositories;

public class NewsRepository
{
    private readonly AppDbContext _db;

    public NewsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<News>> GetAllAsync(string? category = null)
    {
        var query = _db.News.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(category) && category != "Tümü")
        {
            query = query.Where(x => x.Category == category);
        }
        return await query.OrderByDescending(x => x.IsHot)
            .ThenByDescending(x => x.Created)
            .ToListAsync();
    }

    public async Task<News?> GetBySlugAsync(string slug)
    {
        return await _db.News
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<News> CreateAsync(News entity)
    {
        _db.News.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }
}
