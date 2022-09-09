using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public abstract class GenericRepository<T> : IRepository<T>, ISpecificationRepository<T>, IScopedDependency where T : class
{
    private readonly DbSet<T> _dbSet;

    private readonly ISpecificationEvaluator _specificationEvaluator;

    public GenericRepository(DbSet<T> dbSet)
    {
        _dbSet = dbSet;
        _specificationEvaluator = SpecificationEvaluator.Default;
    }

    #region Find

    public virtual T? Find(params object?[]? keyValues)
    {
        return _dbSet.Find(keyValues);
    }

    public virtual async Task<T?> FindAsync(params object?[]? keyValues)
    {
        return await _dbSet.FindAsync(keyValues);
    }

    public virtual async Task<T?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(keyValues, cancellationToken);
    }

    #endregion

    #region List

    public virtual List<T> List()
    {
        return _dbSet.ToList();
    }

    public virtual async Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    #endregion

    #region Count
    public virtual int Count()
    {
        return _dbSet.Count();
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    #endregion

    #region Add

    public virtual T Add(T entity)
    {
        return _dbSet.Add(entity).Entity;
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        return (await _dbSet.AddAsync(entity, cancellationToken)).Entity;
    }

    public virtual void AddRange(params T[] entities)
    {
        _dbSet.AddRange(entities);
    }

    public virtual void AddRange(IEnumerable<T> entities)
    {
        _dbSet.AddRange(entities);
    }

    public virtual async Task AddRangeAsync(params T[] entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public virtual async Task AddRangeAsync(T[] entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    #endregion

    #region Remove

    public virtual T Remove(T entity)
    {
        return _dbSet.Remove(entity).Entity;
    }

    public virtual void RemoveRange(params T[] entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    #endregion

    #region Update

    public virtual T Update(T entity)
    {
        return _dbSet.Update(entity).Entity;
    }

    public virtual void UpdateRange(params T[] entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    #endregion

    #region Specifications

    public virtual async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<TResult?> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var queryResult = await ApplySpecification(specification).ToListAsync(cancellationToken);

        return specification.PostProcessingAction == null ? queryResult : specification.PostProcessingAction(queryResult).ToList();
    }

    public virtual async Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        var queryResult = await ApplySpecification(specification).ToListAsync(cancellationToken);

        return specification.PostProcessingAction == null ? queryResult : specification.PostProcessingAction(queryResult).ToList();
    }

    public virtual async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification, true).CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification, true).AnyAsync(cancellationToken);
    }

    protected virtual IQueryable<T> ApplySpecification(ISpecification<T> specification, bool evaluateCriteriaOnly = false)
    {
        return _specificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification, evaluateCriteriaOnly);
    }

    protected virtual IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification)
    {
        return _specificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
    }

    #endregion
}
