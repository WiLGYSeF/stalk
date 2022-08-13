using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.BackgroundJobs;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class BackgroundJobRepository : GenericRepository<BackgroundJob>, IBackgroundJobRepository
{
    private readonly DbSet<BackgroundJob> _dbSet;

    public BackgroundJobRepository(DbSet<BackgroundJob> dbSet) : base(dbSet)
    {
        _dbSet = dbSet;
    }
}
