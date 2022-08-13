using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobManager : ITransientDependency
{
    Task<BackgroundJob> EnqueueJobAsync(BackgroundJob job, bool saveChanges);

    Task<BackgroundJob> EnqueueOrReplaceJobAsync(BackgroundJob job, bool saveChanges);

    Task<BackgroundJob> EnqueueOrReplaceJobAsync(BackgroundJob job, bool saveChanges, Func<BackgroundJobArgs, BackgroundJobArgs, bool> compareTo);

    Task<BackgroundJob?> FindJob(long id);

    Task<BackgroundJob?> GetNextPriorityJobAsync();

    Task UpdateJobAsync(BackgroundJob job);

    Task DeleteJobAsync(BackgroundJob job);
}
