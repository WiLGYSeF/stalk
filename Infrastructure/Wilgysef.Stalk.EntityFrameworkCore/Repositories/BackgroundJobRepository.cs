using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.BackgroundJobs;

namespace Wilgysef.Stalk.EntityFrameworkCore.Repositories;

public class BackgroundJobRepository : GenericRepository<BackgroundJob>, IBackgroundJobRepository
{
    public BackgroundJobRepository(IStalkDbContext dbContext) : base(dbContext) { }
}
