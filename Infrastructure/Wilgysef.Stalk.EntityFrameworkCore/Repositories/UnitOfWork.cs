using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class UnitOfWork : IUnitOfWork
{
    public IJobRepository JobRepository { get; }

    private readonly IStalkDbContext _dbContext;

    public UnitOfWork(IStalkDbContext dbContext)
    {
        _dbContext = dbContext;
        JobRepository = new JobRepository(_dbContext.Jobs);
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
