using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public interface IJobTaskStateManager : ITransientDependency
{
    /// <summary>
    /// Stops active job task.
    /// </summary>
    /// <param name="jobTask">Task.</param>
    /// <returns></returns>
    Task StopJobTaskAsync(JobTask jobTask);

    /// <summary>
    /// Pauses active job task.
    /// </summary>
    /// <param name="jobTask">Task.</param>
    /// <returns></returns>
    Task PauseJobTaskAsync(JobTask jobTask);

    /// <summary>
    /// Unpauses paused job.
    /// </summary>
    /// <param name="jobTask">Task.</param>
    /// <returns></returns>
    Task UnpauseJobTaskAsync(JobTask jobTask);
}
