using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerServices;

public interface IJobWorkerCollectionService : ISingletonDependency
{
    /// <summary>
    /// Job workers.
    /// </summary>
    IReadOnlyCollection<IJobWorker> Workers { get; }

    /// <summary>
    /// Adds a job worker.
    /// </summary>
    /// <param name="worker">Job worker.</param>
    /// <param name="task">Job worker task.</param>
    /// <param name="cancellationTokenSource">Job worker task cancellation token source.</param>
    void AddJobWorker(IJobWorker worker, Task task, CancellationTokenSource cancellationTokenSource);

    /// <summary>
    /// Removes a job worker.
    /// </summary>
    /// <param name="worker">Job worker.</param>
    void RemoveJobWorker(IJobWorker worker);

    /// <summary>
    /// Gets a job worker.
    /// </summary>
    /// <param name="job">Job the job worker is working on.</param>
    /// <returns>Job worker.</returns>
    IJobWorker? GetJobWorker(Job job);

    /// <summary>
    /// Gets active jobs.
    /// </summary>
    /// <returns>Enumerable of active jobs.</returns>
    IEnumerable<Job> GetActiveJobs();

    /// <summary>
    /// Cancels the job worker token.
    /// </summary>
    /// <param name="worker">Job worker.</param>
    void CancelJobWorkerToken(IJobWorker worker);

    /// <summary>
    /// Gets the job worker task.
    /// </summary>
    /// <param name="worker">Job worker.</param>
    /// <returns>Job worker task.</returns>
    Task GetJobWorkerTask(IJobWorker worker);
}
