using Ardalis.Specification;
using System.Linq.Expressions;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Specifications;

public class JobQuerySpecification : Specification<Job>
{
    public JobQuerySpecification(JobQuery query)
    {
        Query
            .Include(j => j.Tasks)
            .Where(j =>
                (query.Name == null || j.Name != null && j.Name.Contains(query.Name))
                && (query.States.Count == 0 || query.States.Contains(j.State))
                && (!query.StartedBefore.HasValue || j.Started.HasValue && j.Started < query.StartedBefore)
                && (!query.StartedAfter.HasValue || j.Started.HasValue && j.Started > query.StartedAfter)
                && (!query.FinishedBefore.HasValue || j.Finished.HasValue && j.Finished < query.FinishedBefore)
                && (!query.FinishedAfter.HasValue || j.Finished.HasValue && j.Finished > query.FinishedAfter));

        Expression<Func<Job, object?>> sort = query.Sort switch
        {
            JobSortOrder.Id => j => j.Id,
            JobSortOrder.Name => j => j.Name,
            JobSortOrder.State => j => j.State,
            JobSortOrder.Priority => j => j.Priority,
            JobSortOrder.Started => j => j.Started,
            JobSortOrder.Finished => j => j.Finished,
            JobSortOrder.TaskCount => j => j.Tasks.Count,
            _ => throw new NotImplementedException(),
        };

        if (query.SortDescending)
        {
            Query.OrderByDescending(sort);
        }
        else
        {
            Query.OrderBy(sort);
        }
    }
}
