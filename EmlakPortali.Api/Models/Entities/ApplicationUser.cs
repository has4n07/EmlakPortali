using Microsoft.AspNetCore.Identity;

namespace EmlakPortali.Api.Models.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }

    public string? FullName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

