using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkers;

public interface IJobWorker
{
    /// <summary>
    /// Job being worked on.
    /// </summary>
    Job Job { get; }

    /// <summary>
    /// Maximum number of concurrent <see cref="IJobTaskWorker"/>.
    /// </summary>
    int WorkerLimit { get; set; }

    /// <summary>
    /// Waiting for task completion timeout.
    /// </summary>
    TimeSpan TaskWaitTimeout { get; set; }

    /// <summary>
    /// Does work on the job.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task WorkAsync(CancellationToken cancellationToken = default);
}
