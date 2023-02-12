using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobCollectionService : IBackgroundJobCollectionService, ISingletonDependency
{
    public IReadOnlyCollection<BackgroundJob> ActiveJobs => (IReadOnlyCollection<BackgroundJob>)_backgroundTasks.Keys;

    private readonly ConcurrentDictionary<BackgroundJob, bool> _backgroundTasks = new();

    public bool AddActiveJob(BackgroundJob job)
    {
        return _backgroundTasks.TryAdd(job, true);
    }

    public void RemoveActiveJob(BackgroundJob job)
    {
        _backgroundTasks.TryRemove(job, out _);
    }
}
