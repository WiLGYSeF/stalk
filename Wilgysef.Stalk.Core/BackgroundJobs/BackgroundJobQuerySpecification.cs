using Ardalis.Specification;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobQuerySpecification : Specification<BackgroundJob>
{
    public BackgroundJobQuerySpecification(IEnumerable<BackgroundJob> activeJobs, BackgroundJob job)
    {
        var jobIds = activeJobs.Select(j => j.Id).ToList();

        Query
            .Where(j => j.JobArgsName == job.JobArgsName
                && !j.Abandoned
                && !jobIds.Contains(j.Id));
    }
}
