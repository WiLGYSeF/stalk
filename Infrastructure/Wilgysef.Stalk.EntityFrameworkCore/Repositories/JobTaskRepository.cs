using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class JobTaskRepository : GenericRepository<JobTask>, IJobTaskRepository
{
    private readonly DbSet<JobTask> _dbSet;

    public JobTaskRepository(DbSet<JobTask> dbSet) : base(dbSet)
    {
        _dbSet = dbSet;
    }
}
