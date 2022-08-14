using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobStateManager : ITransientDependency
{
    /// <summary>
    /// Stops active job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns></returns>
    Task StopJobAsync(Job job);

    /// <summary>
    /// Pauses active job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns></returns>
    Task PauseJobAsync(Job job);

    /// <summary>
    /// Unpauses paused job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns></returns>
    Task UnpauseJobAsync(Job job);

    /// <summary>
    /// Stops active job task.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="task">Task.</param>
    /// <returns></returns>
    Task StopJobTaskAsync(Job job, JobTask task);

    /// <summary>
    /// Pauses active job task.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="task">Task.</param>
    /// <returns></returns>
    Task PauseJobTaskAsync(Job job, JobTask task);

    /// <summary>
    /// Unpauses paused job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="task">Task.</param>
    /// <returns></returns>
    Task UnpauseJobTaskAsync(Job job, JobTask task);
}
