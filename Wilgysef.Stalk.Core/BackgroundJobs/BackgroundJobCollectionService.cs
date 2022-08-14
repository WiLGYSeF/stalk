using System.Collections.Concurrent;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobCollectionService : IBackgroundJobCollectionService
{
    public IReadOnlyCollection<BackgroundJob> ActiveJobs => (IReadOnlyCollection<BackgroundJob>)_backgroundTasks.Keys;

    private readonly ConcurrentDictionary<BackgroundJob, bool> _backgroundTasks = new();

    public void AddActiveJob(BackgroundJob job)
    {
        _backgroundTasks[job] = true;
    }

    public void RemoveActiveJob(BackgroundJob job)
    {
        _backgroundTasks.Remove(job, out _);
    }
}
