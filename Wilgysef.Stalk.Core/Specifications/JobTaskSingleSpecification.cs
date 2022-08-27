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
    /// <param name="readOnly">Indicates if the query is intended for read only.</param>
    public JobTaskSingleSpecification(long jobTaskId, bool readOnly = false)
    {
        Query
            .Include(t => t.Job)
                // TODO: FIX!
                //.ThenInclude(j => j.Tasks)
            .Where(t => t.Id == jobTaskId);

        if (readOnly)
        {
            Query.AsNoTracking();
        }
    }
}
