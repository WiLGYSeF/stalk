using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkers;

public interface IJobWorker
{
    Job? Job { get; }

    IJobWorker WithJob(Job job);

    Task WorkAsync(CancellationToken cancellationToken = default);
}
