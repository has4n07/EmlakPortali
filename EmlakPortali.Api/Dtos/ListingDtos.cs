using EmlakPortali.Api.Models.Entities;

namespace EmlakPortali.Api.Dtos;

public enum ListingOrderBy
{
    Newest = 1,
    PriceAsc = 2,
    PriceDesc = 3
}

public class ListingSearchQueryDto : PagedRequestDto
{
    public string? Q { get; set; }
    public int? CityId { get; set; }
    public int? DistrictId { get; set; }
    public ListingType? ListingType { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? RoomCount { get; set; }
    public int? BathCount { get; set; }
    public int? MinAreaM2 { get; set; }
    public int? MaxAreaM2 { get; set; }
    public ListingOrderBy OrderBy { get; set; } = ListingOrderBy.Newest;
}

public class ListingCreateDto
{
    public ListingType ListingType { get; set; }
    public int CategoryId { get; set; }
    public int CityId { get; set; }
    public int DistrictId { get; set; }
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
}

public class ListingUpdateDto : ListingCreateDto
{
    public bool IsActive { get; set; } = true;
}

public class ListingListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Price { get; set; }
    public ListingType ListingType { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string CityName { get; set; } = null!;
    public string DistrictName { get; set; } = null!;
    public string? CoverImageUrl { get; set; }
    public DateTime Created { get; set; }
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
}

public class ListingDetailDto : ListingListItemDto
{
    public string Description { get; set; } = null!;
    public int CityId { get; set; }
    public int DistrictId { get; set; }
    public int? RoomCount { get; set; }
    public int? BathCount { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? AreaGrossM2 { get; set; }
    public int? AreaNetM2 { get; set; }
    public string? AddressLine { get; set; }
    public List<ListingImageDto> Images { get; set; } = new();
    public Guid OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
}

public class ListingImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public int SortOrder { get; set; }
}

public class ListingImageCreateDto
{
    public string Url { get; set; } = null!;
    public int SortOrder { get; set; } = 100;
}

public class ListingImageUpdateDto
{
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

