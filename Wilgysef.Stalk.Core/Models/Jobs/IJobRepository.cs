namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobRepository : IRepository<Job>
{
    // TODO: replace with specification
    IQueryable<Job> GetJobs();
}
