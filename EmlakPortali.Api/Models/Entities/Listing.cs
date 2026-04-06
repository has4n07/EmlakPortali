namespace EmlakPortali.Api.Models.Entities;

public class Listing : BaseEntity
{
    public int Id { get; set; }

    public Guid OwnerUserId { get; set; }
    public ApplicationUser OwnerUser { get; set; } = null!;

    public ListingType ListingType { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public int DistrictId { get; set; }
    public District District { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public decimal Price { get; set; }
    public int? RoomCount { get; set; }
    public int? BathCount { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? AreaGrossM2 { get; set; }
    public int? AreaNetM2 { get; set; }

    public string? AddressLine { get; set; }
    public string? CoverImageUrl { get; set; }

    public bool IsApproved { get; set; } = false;

    public List<ListingImage> Images { get; set; } = new();
}

