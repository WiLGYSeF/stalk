using Ardalis.Specification;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.Specifications;

public class JobSingleSpecification : Specification<Job>
{
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
