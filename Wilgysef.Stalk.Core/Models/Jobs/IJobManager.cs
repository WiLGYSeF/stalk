using Ardalis.Specification;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobManager : ITransientDependency
{
    Task<Job> CreateJobAsync(Job job, CancellationToken cancellationToken = default);

    Task<Job> GetJobAsync(long id, CancellationToken cancellationToken = default);

    Task<Job> GetJobByTaskIdAsync(long id, CancellationToken cancellationToken = default);

    Task<List<Job>> GetJobsAsync(CancellationToken cancellationToken = default);

    Task<List<Job>> GetJobsAsync(ISpecification<Job> specification, CancellationToken cancellationToken = default);

    Task<Job?> GetNextPriorityJobAsync(CancellationToken cancellationToken = default);

    Task<Job> UpdateJobAsync(Job job, CancellationToken cancellationToken = default);

    Task DeleteJobAsync(Job job, CancellationToken cancellationToken = default);

    Task SetJobActiveAsync(Job job, CancellationToken cancellationToken = default);

    Task SetJobDoneAsync(Job job, CancellationToken cancellationToken = default);

    Task DeactivateJobsAsync(CancellationToken cancellationToken = default);
}
