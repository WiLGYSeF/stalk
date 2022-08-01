using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Interfaces;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobManager : ITransientDependency
{
    Task<Job> CreateJobAsync(Job job);

    Task<Job> GetJobAsync(long id);

    Task<List<Job>> GetJobsAsync();

    Task<Job> UpdateJobAsync(Job job);

    Task DeleteJobAsync(Job job, bool force = false);

    Task StopJobAsync(Job job, bool force = false);

    Task PauseJobAsync(Job job, bool force = false);

    Task UnpauseJobAsync(Job job);

    Task DeleteJobTaskAsync(Job job, JobTask task, bool force = false);

    Task StopJobTaskAsync(Job job, JobTask task, bool force = false);

    Task PauseJobTaskAsync(Job job, JobTask task, bool force = false);

    Task UnpauseJobTaskAsync(JobTask task);
}
