using Ardalis.Specification;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.Specifications;

/// <summary>
/// Gets a single job task by job task Id.
/// </summary>
public class JobTaskSingleSpecification : Specification<JobTask>
{
    /// <summary>
    /// Gets a single job task by either job task Id.
    /// </summary>
    /// <param name="jobTaskId">Job task Id.</param>
    public JobTaskSingleSpecification(long jobTaskId)
    {
        Query
            .Include(t => t.Job)
                .ThenInclude(j => j.Tasks)
            .Where(t => t.Id == jobTaskId);
    }
}
