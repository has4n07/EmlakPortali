namespace EmlakPortali.Api.Models.Entities;

public class District : BaseEntity
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public string Name { get; set; } = null!;

    public City City { get; set; } = null!;
}

