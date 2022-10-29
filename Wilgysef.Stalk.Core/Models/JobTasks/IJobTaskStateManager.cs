namespace Wilgysef.Stalk.Core.Models.JobTasks;

public interface IJobTaskStateManager
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
