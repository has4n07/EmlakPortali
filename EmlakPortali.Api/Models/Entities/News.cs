namespace EmlakPortali.Api.Models.Entities;

public class News : BaseEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Summary { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string Category { get; set; } = null!;
    public bool IsHot { get; set; } = false;
    public int ReadMinutes { get; set; } = 5;
}

