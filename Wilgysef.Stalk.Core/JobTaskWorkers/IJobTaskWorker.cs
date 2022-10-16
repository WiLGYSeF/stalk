using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public interface IJobTaskWorker
{
    /// <summary>
    /// Job task being worked on.
    /// </summary>
    JobTask JobTask { get; }

    /// <summary>
    /// Does work on the job task.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task WorkAsync(CancellationToken cancellationToken = default);
}
