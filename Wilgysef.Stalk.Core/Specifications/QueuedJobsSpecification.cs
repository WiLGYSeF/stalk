using Ardalis.Specification;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.Specifications;

/// <summary>
/// Get queued jobs in highest priority order.
/// </summary>
public class QueuedJobsSpecification : Specification<Job>
{
    /// <summary>
    /// Get queued jobs in highest priority order.
    /// </summary>
    /// <param name="readOnly">Indicates if the query is intended for read only.</param>
    public QueuedJobsSpecification(bool readOnly = false)
    {
        Query
            .Include(j => j.Tasks)
            .Where(Job.IsQueuedExpression)
            .Where(j => j.Tasks.AsQueryable()
                .Any(JobTask.IsQueuedExpression))
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.Started);

        if (readOnly)
        {
            Query.AsNoTracking();
        }
    }
}
