using Ardalis.Specification;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

/// <summary>
/// Gets jobs that will no longer run that haven't yet been abandoned.
/// </summary>
public class ExpiredBackgroundJobSpecification : Specification<BackgroundJob>
{
    /// <summary>
    /// Gets jobs that will no longer run that haven't yet been abandoned.
    /// </summary>
    /// <param name="activeJobs">List of active jobs.</param>
    /// <param name="now">Current time.</param>
    public ExpiredBackgroundJobSpecification(IEnumerable<BackgroundJob> activeJobs, DateTime? now = null)
    {
        var jobIds = activeJobs.Select(j => j.Id).ToList();
        now ??= DateTime.Now;

        Query
            .Where(BackgroundJob.IsScheduledExpression)
            .Where(j =>
                (j.MaxAttempts.HasValue && j.Attempts >= j.MaxAttempts
                    || j.MaximumLifetime.HasValue && j.MaximumLifetime <= now)
                && !jobIds.Contains(j.Id));
    }
}
