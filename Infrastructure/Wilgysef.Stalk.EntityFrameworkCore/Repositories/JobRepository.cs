using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class JobRepository : GenericRepository<Job>, IJobRepository
{
    public JobRepository(IStalkDbContext dbContext) : base(dbContext) { }
}
