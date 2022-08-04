using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkers;

public interface IJobWorker
{
    JobWorker WithJob(Job job);

    Task WorkAsync(CancellationToken? cancellationToken = null);
}
