namespace EmlakPortali.Api.Models.Entities;

public abstract class BaseEntity
{
    public bool IsActive { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
}

