using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerServices;

public interface IJobWorkerCollectionService : ISingletonDependency
{
    /// <summary>
    /// Job workers.
    /// </summary>
    IReadOnlyCollection<JobWorker> Workers { get; }

    /// <summary>
    /// Active jobs.
    /// </summary>
    IReadOnlyCollection<Job> Jobs { get; }

    void AddJobWorker(JobWorker worker, Task task, CancellationTokenSource cancellationTokenSource);

    JobWorker? GetJobWorker(Job job);

    void CancelJobWorkerToken(JobWorker worker);

    Task GetJobWorkerTask(JobWorker worker);
}
