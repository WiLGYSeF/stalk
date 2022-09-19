using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class UnitOfWork : IUnitOfWork, IScopedDependency
{
    private readonly IStalkDbContext _dbContext;

    public UnitOfWork(IStalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public int SaveChanges()
    {
        return _dbContext.SaveChanges();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
