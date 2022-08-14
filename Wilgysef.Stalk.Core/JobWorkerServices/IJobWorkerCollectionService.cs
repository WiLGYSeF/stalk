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

    /// <summary>
    /// Adds a job worker.
    /// </summary>
    /// <param name="worker">Job worker.</param>
    /// <param name="task">Job worker task.</param>
    /// <param name="cancellationTokenSource">Job worker task cancellation token source.</param>
    void AddJobWorker(JobWorker worker, Task task, CancellationTokenSource cancellationTokenSource);

    /// <summary>
    /// Removes a job worker.
    /// </summary>
    /// <param name="worker">Job worker.</param>
    void RemoveJobWorker(JobWorker worker);

    /// <summary>
    /// Gets a job worker.
    /// </summary>
    /// <param name="job">Job the job worker is working on.</param>
    /// <returns>Job worker.</returns>
    JobWorker? GetJobWorker(Job job);

    /// <summary>
    /// Cancels the job worker token.
    /// </summary>
    /// <param name="worker">Job worker.</param>
    void CancelJobWorkerToken(JobWorker worker);

    /// <summary>
    /// Gets the job worker task.
    /// </summary>
    /// <param name="worker">Job worker.</param>
    /// <returns>Job worker task.</returns>
    Task GetJobWorkerTask(JobWorker worker);
}
