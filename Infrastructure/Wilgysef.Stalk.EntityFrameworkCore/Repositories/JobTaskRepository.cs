using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class JobTaskRepository : GenericRepository<JobTask>, IJobTaskRepository
{
    public JobTaskRepository(IStalkDbContext dbContext) : base(dbContext) { }
}
