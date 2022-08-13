using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.EntityFrameworkCore;

public interface IStalkDbContext : IScopedDependency
{
    DbSet<Job> Jobs { get; set; }

    DbSet<JobTask> JobTasks { get; set; }

    DbSet<BackgroundJob> BackgroundJobs { get; set; }

    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
