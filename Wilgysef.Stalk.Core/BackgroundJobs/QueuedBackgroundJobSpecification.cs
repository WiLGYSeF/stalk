using Ardalis.Specification;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class QueuedBackgroundJobSpecification : Specification<BackgroundJob>
{
    public QueuedBackgroundJobSpecification(IEnumerable<BackgroundJob> activeJobs, DateTime? now = null)
    {
        var jobIds = activeJobs.Select(j => j.Id).ToList();
        now ??= DateTime.Now;

        Query
            .Where(j => !j.Abandoned
                && (j.NextRun == null || j.NextRun <= now)
                && (j.MaximumLifetime == null || j.MaximumLifetime > now)
                && !jobIds.Contains(j.Id))
            .OrderByDescending(j => j.Priority);
    }
}
