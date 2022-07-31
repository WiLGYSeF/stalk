using Wilgysef.Stalk.Core.Shared.Interfaces;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobManager : ITransientDependency
{
    Task<Job> CreateJobAsync(Job job);
}
