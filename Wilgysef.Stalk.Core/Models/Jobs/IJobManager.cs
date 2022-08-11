using Ardalis.Specification;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobManager : ITransientDependency
{
    Task<Job> CreateJobAsync(Job job);

    Task<Job> GetJobAsync(long id);

    Task<Job> GetJobByTaskIdAsync(long id);

    Task<List<Job>> GetJobsAsync();

    Task<List<Job>> GetJobsAsync(ISpecification<Job> specification);

    Task<Job?> GetNextPriorityJobAsync();

    Task<Job> UpdateJobAsync(Job job);

    Task DeleteJobAsync(Job job);

    Task SetJobActiveAsync(Job job);

    Task SetJobDoneAsync(Job job);

    Task DeactivateJobsAsync();
}
