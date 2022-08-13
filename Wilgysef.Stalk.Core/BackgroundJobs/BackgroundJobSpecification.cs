using Ardalis.Specification;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobSpecification : Specification<BackgroundJob>
{
    public BackgroundJobSpecification(IEnumerable<BackgroundJob> activeJobs)
    {
        var jobIds = activeJobs.Select(j => j.Id).ToList();
        var now = DateTime.Now;

        Query
            .Where(j => !jobIds.Contains(j.Id)
                && !j.Abandoned
                && (j.NextRun == null || j.NextRun <= now)
                && (j.MaximumLifetime == null || j.MaximumLifetime > now))
            .OrderByDescending(j => j.Priority);
    }
}
