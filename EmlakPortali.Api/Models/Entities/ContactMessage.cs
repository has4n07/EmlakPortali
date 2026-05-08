using System;
namespace EmlakPortali.Api.Models.Entities;

public class ContactMessage : BaseEntity
{
    public int Id { get; set; }
    
    public Guid ReceiverId { get; set; }
    public ApplicationUser Receiver { get; set; } = null!;
    
    public int? ListingId { get; set; }
    public Listing? Listing { get; set; }
    
    public string SenderName { get; set; } = null!;
    public string SenderEmail { get; set; } = null!;
    public string SenderPhone { get; set; } = null!;
    
    public string Message { get; set; } = null!;
    
    public bool IsRead { get; set; } = false;
}
