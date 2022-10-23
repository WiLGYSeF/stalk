using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.EntityFrameworkCore;

public interface IStalkDbContext : IDisposable
{
    DbSet<Job> Jobs { get; set; }

    DbSet<JobTask> JobTasks { get; set; }

    DbSet<BackgroundJob> BackgroundJobs { get; set; }

    event EventHandler<SavingChangesEventArgs>? SavingChanges;

    event EventHandler<SavedChangesEventArgs>? SavedChanges;

    event EventHandler<SaveChangesFailedEventArgs>? SaveChangesFailed;

    DbSet<T> Set<T>() where T : class;

    DbSet<T> Set<T>(string name) where T : class;

    EntityEntry<T> Entry<T>(T entity) where T : class;

    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    ValueTask DisposeAsync();
}
