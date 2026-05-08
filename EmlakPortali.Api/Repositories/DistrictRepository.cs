using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models.Entities;

namespace EmlakPortali.Api.Repositories;

public class DistrictRepository : GenericRepository<District>
{
    public DistrictRepository(AppDbContext db) : base(db)
    {
    }
}
