using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class UnitOfWork : IUnitOfWork
{
    public IJobRepository JobRepository { get; }

    public IJobTaskRepository JobTaskRepository { get; }

    public IBackgroundJobRepository BackgroundJobRepository { get; }

    private readonly IStalkDbContext _dbContext;

    public UnitOfWork(IStalkDbContext dbContext)
    {
        _dbContext = dbContext;
        JobRepository = new JobRepository(_dbContext.Jobs);
        JobTaskRepository = new JobTaskRepository(_dbContext.JobTasks);
        BackgroundJobRepository = new BackgroundJobRepository(_dbContext.BackgroundJobs);
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
