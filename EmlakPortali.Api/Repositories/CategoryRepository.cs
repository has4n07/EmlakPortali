using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models.Entities;

namespace EmlakPortali.Api.Repositories;

public class CategoryRepository : GenericRepository<Category>
{
    public CategoryRepository(AppDbContext db) : base(db)
    {
    }
}
