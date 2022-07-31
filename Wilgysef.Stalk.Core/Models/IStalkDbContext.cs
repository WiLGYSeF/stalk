using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Interfaces;

namespace Wilgysef.Stalk.Core.Models;

public interface IStalkDbContext : IScopedDependency
{
    DbSet<Job> Jobs { get; set; }
    
    DbSet<JobTask> JobTasks { get; set; }

    int SaveChanges();
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
