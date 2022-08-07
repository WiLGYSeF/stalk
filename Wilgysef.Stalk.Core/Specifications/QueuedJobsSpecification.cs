using Ardalis.Specification;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.Specifications;

public class QueuedJobsSpecification : Specification<Job>
{
    public QueuedJobsSpecification()
    {
        Query
            .Include(j => j.Tasks)
            .Where(Job.IsQueuedExpression)
            .Where(j => j.Tasks.AsQueryable()
                .Any(JobTask.IsQueuedExpression))
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.Started);
    }
}
