namespace EmlakPortali.Api.Models.Entities;

public class Project : BaseEntity
{
    public int Id { get; set; }
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

