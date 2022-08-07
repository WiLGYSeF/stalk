using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.Models;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public abstract class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet;

    public GenericRepository(DbSet<T> dbSet)
    {
        _dbSet = dbSet;
    }

    #region Find

    public T? Find(params object?[]? keyValues)
    {
        return _dbSet.Find(keyValues);
    }

    public async Task<T?> FindAsync(params object?[]? keyValues)
    {
        return await _dbSet.FindAsync(keyValues);
    }

    public async Task<T?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(keyValues, cancellationToken);
    }

    #endregion

    #region Add

    public T Add(T entity)
    {
        return _dbSet.Add(entity).Entity;
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        return (await _dbSet.AddAsync(entity, cancellationToken)).Entity;
    }

    public void AddRange(params T[] entities)
    {
        _dbSet.AddRange(entities);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        _dbSet.AddRange(entities);
    }

    public async Task AddRangeAsync(params T[] entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public async Task AddRangeAsync(T[] entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    #endregion

    #region Remove

    public T Remove(T entity)
    {
        return _dbSet.Remove(entity).Entity;
    }

    public void RemoveRange(params T[] entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    #endregion

    #region Update

    public T Update(T entity)
    {
        return _dbSet.Update(entity).Entity;
    }

    public void UpdateRange(params T[] entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    #endregion
}
