using EmlakPortali.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EmlakPortali.Api.Repositories;

public class GenericRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext db)
    {
        _db = db;
        _dbSet = _db.Set<T>();
    }

    public async Task<List<T>> GetAllAsync(bool trackChanges = false)
    {
        var query = trackChanges ? _dbSet : _dbSet.AsNoTracking();
        return await query.ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<T?> GetByConditionAsync(Expression<Func<T, bool>> expression, bool trackChanges = false)
    {
        var query = trackChanges ? _dbSet : _dbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(expression);
    }

    public async Task<List<T>> GetListByConditionAsync(Expression<Func<T, bool>> expression, bool trackChanges = false)
    {
        var query = trackChanges ? _dbSet : _dbSet.AsNoTracking();
        return await query.Where(expression).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
