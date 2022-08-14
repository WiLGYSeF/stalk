using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobTaskWorkerServices;

public interface IJobTaskWorkerCollectionService : ISingletonDependency
{
    /// <summary>
    /// Job task workers.
    /// </summary>
    IReadOnlyCollection<JobTaskWorker> Workers { get; }

    /// <summary>
    /// Adds a job task worker.
    /// </summary>
    /// <param name="worker">Job task worker.</param>
    /// <param name="task">Job task worker task.</param>
    /// <param name="cancellationTokenSource">Job task worker task cancellation token source.</param>
    void AddJobTaskWorker(JobTaskWorker worker, Task task, CancellationTokenSource cancellationTokenSource);

    /// <summary>
    /// Removes a job task worker.
    /// </summary>
    /// <param name="worker">Job task worker.</param>
    void RemoveJobTaskWorker(JobTaskWorker worker);

    /// <summary>
    /// Gets a job task worker.
    /// </summary>
    /// <param name="jobTask">Job task the job task worker is working on.</param>
    /// <returns>Job task worker.</returns>
    JobTaskWorker? GetJobTaskWorker(JobTask jobTask);

    /// <summary>
    /// Gets active job tasks.
    /// </summary>
    /// <returns>Enumerable of active job tasks.</returns>
    IEnumerable<JobTask> GetActiveJobTasks();

    /// <summary>
    /// Cancels the job task worker token.
    /// </summary>
    /// <param name="worker">Job task worker.</param>
    void CancelJobTaskWorkerToken(JobTaskWorker worker);

    /// <summary>
    /// Gets the job task worker task.
    /// </summary>
    /// <param name="worker">Job task worker.</param>
    /// <returns>Job task worker task.</returns>
    Task GetJobTaskWorkerTask(JobTaskWorker worker);
}
