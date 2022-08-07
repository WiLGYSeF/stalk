using Ardalis.Specification;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.Specifications;

public class JobSingleSpecification : Specification<Job>
{
    public JobSingleSpecification(long id)
    {
        Query
            .Include(j => j.Tasks)
            .Where(j => j.Id == id);
    }
}
