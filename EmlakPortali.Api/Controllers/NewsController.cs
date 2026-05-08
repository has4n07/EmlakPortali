using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Models.Entities;
using EmlakPortali.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NewsController : ControllerBase
{
    private readonly NewsRepository _newsRepository;

    public NewsController(NewsRepository newsRepository)
    {
        _newsRepository = newsRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? category = null)
    {
        var news = await _newsRepository.GetAllAsync(category);
        return Ok(new DataResult<object> { Status = true, Message = "OK", Data = news });
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var news = await _newsRepository.GetBySlugAsync(slug);
        if (news == null)
        {
            return NotFound(new DataResult<object> { Status = false, Message = "Haber bulunamadı." });
        }
        return Ok(new DataResult<object> { Status = true, Message = "OK", Data = news });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(NewsCreateDto dto)
    {
        var entity = new News
        {
            Title = dto.Title,
            Slug = dto.Slug,
            Summary = dto.Summary,
            Content = dto.Content,
            ImageUrl = dto.ImageUrl,
            Category = dto.Category,
            IsHot = dto.IsHot,
            ReadMinutes = dto.ReadMinutes,
            Created = DateTime.UtcNow
        };

        var result = await _newsRepository.CreateAsync(entity);
        return Ok(new DataResult<object> { Status = true, Message = "Haber oluşturuldu.", Data = new { result.Id } });
    }
}

public class NewsCreateDto
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Summary { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string Category { get; set; } = null!;
    public bool IsHot { get; set; } = false;
    public int ReadMinutes { get; set; } = 5;
}

