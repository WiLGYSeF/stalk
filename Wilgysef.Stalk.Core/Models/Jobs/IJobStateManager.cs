namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobStateManager
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
}
