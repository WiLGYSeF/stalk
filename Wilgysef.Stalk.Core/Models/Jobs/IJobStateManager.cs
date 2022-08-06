using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobStateManager : ITransientDependency
{
    Task StopJobAsync(Job job);

    Task PauseJobAsync(Job job);

    Task UnpauseJobAsync(Job job);

    Task StopJobTaskAsync(Job job, JobTask task);

    Task PauseJobTaskAsync(Job job, JobTask task);

    Task UnpauseJobTaskAsync(Job job, JobTask task);
}
