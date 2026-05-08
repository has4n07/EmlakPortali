using EmlakPortali.Api.Dtos;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Models.Entities;
using EmlakPortali.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmlakPortali.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectsController : ControllerBase
{
    private readonly ProjectRepository _projectRepository;

    public ProjectsController(ProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? city = null)
    {
        var projects = await _projectRepository.GetAllAsync(city);
        return Ok(new DataResult<object> { Status = true, Message = "OK", Data = projects });
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var project = await _projectRepository.GetBySlugAsync(slug);
        if (project == null)
        {
            return NotFound(new DataResult<object> { Status = false, Message = "Proje bulunamadı." });
        }
        return Ok(new DataResult<object> { Status = true, Message = "OK", Data = project });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(ProjectCreateDto dto)
    {
        var entity = new Project
        {
            Name = dto.Name,
            Slug = dto.Slug,
            City = dto.City,
            District = dto.District,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            TotalUnits = dto.TotalUnits,
            DeliveryDate = dto.DeliveryDate,
            MinPrice = dto.MinPrice,
            Status = dto.Status,
            RoomTypes = dto.RoomTypes,
            Created = DateTime.UtcNow
        };

        var result = await _projectRepository.CreateAsync(entity);
        return Ok(new DataResult<object> { Status = true, Message = "Proje oluşturuldu.", Data = new { result.Id } });
    }
}

public class ProjectCreateDto
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? District { get; set; }
    public string Description { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public int TotalUnits { get; set; }
    public string DeliveryDate { get; set; } = null!;
    public decimal MinPrice { get; set; }
    public string Status { get; set; } = "Satışta";
    public string RoomTypes { get; set; } = null!;
}

