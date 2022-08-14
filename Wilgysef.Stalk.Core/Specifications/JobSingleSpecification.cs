using Ardalis.Specification;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.Specifications;

/// <summary>
/// Gets a single job by either job Id or job task Id.
/// </summary>
public class JobSingleSpecification : Specification<Job>
{
    /// <summary>
    /// Gets a single job by either job Id or job task Id.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <param name="taskId">Job task Id.</param>
    /// <exception cref="ArgumentNullException">Both <paramref name="jobId"/> and <paramref name="taskId"/> are null.</exception>
    public JobSingleSpecification(long? jobId = null, long? taskId = null)
    {
        if (!jobId.HasValue && !taskId.HasValue)
        {
            throw new ArgumentNullException(nameof(jobId));
        }

        Query.Include(j => j.Tasks);

        if (jobId.HasValue)
        {
            Query.Where(j => j.Id == jobId);
        }
        else
        {
            Query.Where(j => j.Tasks.Any(t => t.Id == taskId));
        }
    }
}
