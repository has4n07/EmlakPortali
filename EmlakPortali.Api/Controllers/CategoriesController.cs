using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<DataResult<object>> Get()
    {
        var data = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();
            
        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<DataResult<object>> GetForAdmin()
    {
        var data = await _db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.IsActive, Created = x.Created, ListingCount = x.Listings.Count() })
            .ToListAsync();
            
        return new DataResult<object> { Status = true, Message = "OK", Data = data };
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<Result> Create([FromBody] CategoryCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return new Result { Status = false, Message = "Kategori adı boş olamaz." };
        }

        var exists = await _db.Categories.AnyAsync(x => x.Name == dto.Name.Trim());
        if (exists)
        {
            return new Result { Status = false, Message = "Bu isimde bir kategori zaten mevcut." };
        }

        var category = new EmlakPortali.Api.Models.Entities.Category
        {
            Name = dto.Name.Trim(),
            IsActive = true,
            Created = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return new Result { Status = true, Message = "Kategori başarıyla oluşturuldu." };
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<Result> Edit(int id, [FromBody] CategoryCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new Result { Status = false, Message = "Kategori adı boş olamaz." };

        var category = await _db.Categories.FindAsync(id);
        if (category is null)
            return new Result { Status = false, Message = "Kategori bulunamadı." };

        var exists = await _db.Categories.AnyAsync(x => x.Name == dto.Name.Trim() && x.Id != id);
        if (exists)
            return new Result { Status = false, Message = "Bu isimde bir kategori zaten mevcut." };

        category.Name = dto.Name.Trim();
        await _db.SaveChangesAsync();

        return new Result { Status = true, Message = "Kategori başarıyla güncellendi." };
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/active")]
    public async Task<Result> ToggleActive(int id, [FromQuery] bool isActive)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
            return new Result { Status = false, Message = "Kategori bulunamadı." };

        category.IsActive = isActive;
        await _db.SaveChangesAsync();

        return new Result { Status = true, Message = "Kategori durumu güncellendi." };
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<Result> Delete(int id)
    {
        var category = await _db.Categories.Include(x => x.Listings).FirstOrDefaultAsync(x => x.Id == id);
        if (category is null)
        {
            return new Result { Status = false, Message = "Kategori bulunamadı." };
        }

        if (category.Listings.Any())
        {
            return new Result { Status = false, Message = "Bu kategoriye ait ilanlar bulunduğu için silinemez." };
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return new Result { Status = true, Message = "Kategori başarıyla silindi." };
    }
}

public class CategoryCreateDto
{
    public string Name { get; set; } = null!;
}
