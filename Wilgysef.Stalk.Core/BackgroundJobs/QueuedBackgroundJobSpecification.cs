using Ardalis.Specification;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

/// <summary>
/// Gets queued jobs in the highest priority order.
/// </summary>
public class QueuedBackgroundJobSpecification : Specification<BackgroundJob>
{
    /// <summary>
    /// Gets queued jobs in the highest priority order.
    /// </summary>
    /// <param name="activeJobs">List of active jobs.</param>
    /// <param name="now">Current time.</param>
    public QueuedBackgroundJobSpecification(IEnumerable<BackgroundJob> activeJobs, DateTime? now = null)
    {
        var jobIds = activeJobs.Select(j => j.Id).ToList();
        now ??= DateTime.Now;

        Query
            .Where(BackgroundJob.IsScheduledExpression)
            .Where(j => (!j.NextRun.HasValue || j.NextRun <= now)
                && (!j.MaximumLifetime.HasValue || j.MaximumLifetime > now)
                && !jobIds.Contains(j.Id))
            .OrderByDescending(j => j.Priority);
    }
}
