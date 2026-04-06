namespace EmlakPortali.Api.Models.Entities;

public class Category : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    
    public List<Listing> Listings { get; set; } = new();
}
