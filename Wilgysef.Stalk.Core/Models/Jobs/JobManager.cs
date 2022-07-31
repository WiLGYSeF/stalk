namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobManager : IJobManager
{
    private readonly IStalkDbContext _dbContext;

    public JobManager(
        IStalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Job> CreateJobAsync(Job job)
    {
        var entity = (await _dbContext.Jobs.AddAsync(job)).Entity;
        await _dbContext.SaveChangesAsync();
        return entity;
    }
}
