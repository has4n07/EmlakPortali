namespace EmlakPortali.Api.Models.Entities;

public class FavoriteListing : BaseEntity
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
}

