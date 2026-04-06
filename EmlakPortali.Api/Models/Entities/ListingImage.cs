namespace EmlakPortali.Api.Models.Entities;

public class ListingImage : BaseEntity
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public string Url { get; set; } = null!;
    public int SortOrder { get; set; }
}

