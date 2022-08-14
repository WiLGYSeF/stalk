using Ardalis.Specification;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

/// <summary>
/// Gets background jobs of the same type of job.
/// </summary>
public class BackgroundJobQuerySpecification : Specification<BackgroundJob>
{
    /// <summary>
    /// Gets background jobs of the same type of <paramref name="job"/>.
    /// </summary>
    /// <param name="activeJobs">Active jobs.</param>
    /// <param name="job">Job to compare.</param>
    public BackgroundJobQuerySpecification(IEnumerable<BackgroundJob> activeJobs, BackgroundJob job)
    {
        var jobIds = activeJobs.Select(j => j.Id).ToList();

        Query
            .Where(j => j.JobArgsName == job.JobArgsName
                && !j.Abandoned
                && !jobIds.Contains(j.Id));
    }
}
