using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models.Entities;

namespace EmlakPortali.Api.Repositories;

public class CityRepository : GenericRepository<City>
{
    public CityRepository(AppDbContext db) : base(db)
    {
    }
}
