using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobManager : ITransientDependency
{
    Task<BackgroundJob> EnqueueJobAsync(BackgroundJob job, bool saveChanges, CancellationToken cancellationToken = default);

    Task<BackgroundJob> EnqueueOrReplaceJobAsync(BackgroundJob job, bool saveChanges, CancellationToken cancellationToken = default);

    Task<BackgroundJob> EnqueueOrReplaceJobAsync(
        BackgroundJob job,
        bool saveChanges,
        Func<BackgroundJobArgs, BackgroundJobArgs, bool> compareTo,
        CancellationToken cancellationToken = default);

    Task<BackgroundJob?> FindJob(long id, CancellationToken cancellationToken = default);

    Task<BackgroundJob?> GetNextPriorityJobAsync(CancellationToken cancellationToken = default);

    Task UpdateJobAsync(BackgroundJob job, CancellationToken cancellationToken = default);

    Task DeleteJobAsync(BackgroundJob job, CancellationToken cancellationToken = default);
}
