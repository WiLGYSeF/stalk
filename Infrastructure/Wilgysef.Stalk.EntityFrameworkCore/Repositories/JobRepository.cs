using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class JobRepository : GenericRepository<Job>, IJobRepository
{
    private readonly DbSet<Job> _dbSet;

    public JobRepository(DbSet<Job> dbSet) : base(dbSet)
    {
        _dbSet = dbSet;
    }
}
