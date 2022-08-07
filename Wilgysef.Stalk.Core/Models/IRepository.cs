using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models;

public interface IRepository<T> : IScopedDependency where T : class
{
    #region Find

    T? Find(params object?[]? keyValues);

    Task<T?> FindAsync(params object?[]? keyValues);

    Task<T?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken = default);

    #endregion

    #region Add

    T Add(T entity);

    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    void AddRange(params T[] entities);

    void AddRange(IEnumerable<T> entities);

    Task AddRangeAsync(params T[] entities);

    Task AddRangeAsync(T[] entities, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    #endregion

    #region Remove

    T Remove(T entity);

    void RemoveRange(params T[] entities);

    void RemoveRange(IEnumerable<T> entities);

    #endregion

    #region Update

    T Update(T entity);

    void UpdateRange(params T[] entities);

    void UpdateRange(IEnumerable<T> entities);

    #endregion
}
