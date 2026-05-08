using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Repositories;

public class ProjectRepository
{
    private readonly AppDbContext _db;

    public ProjectRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Project>> GetAllAsync(string? city = null)
    {
        var query = _db.Projects.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(x => x.City == city);
        }
        return await query.OrderByDescending(x => x.Created).ToListAsync();
    }

    public async Task<Project?> GetBySlugAsync(string slug)
    {
        return await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<Project> CreateAsync(Project entity)
    {
        _db.Projects.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }
}
