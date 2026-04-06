namespace EmlakPortali.Api.Models.Entities;

public class City : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public List<District> Districts { get; set; } = new();
}

