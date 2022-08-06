using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobStateManager : ITransientDependency
{
    Task StopJobAsync(Job job, bool blocking = false);

    Task PauseJobAsync(Job job, bool blocking = false);

    Task UnpauseJobAsync(Job job);

    Task StopJobTaskAsync(Job job, JobTask task, bool blocking = false);

    Task PauseJobTaskAsync(Job job, JobTask task, bool blocking = false);

    Task UnpauseJobTaskAsync(Job job, JobTask task);
}
