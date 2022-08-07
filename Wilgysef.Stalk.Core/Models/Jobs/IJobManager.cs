using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobManager : ITransientDependency
{
    Task<Job> CreateJobAsync(Job job);

    Task<Job> GetJobAsync(long id);

    Task<List<Job>> GetJobsAsync();

    Task<Job?> GetNextPriorityJobAsync();

    Task<Job> UpdateJobAsync(Job job);

    Task DeleteJobAsync(Job job);

    Task SetJobActiveAsync(Job job);

    Task SetJobDoneAsync(Job job);

    Task DeactivateJobsAsync();
}
