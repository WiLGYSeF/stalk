using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkers;

public interface IJobWorker : IDisposable
{
    Job? Job { get; }

    int TaskWaitTimeoutMilliseconds { get; set; }

    IJobWorker WithJob(Job job);

    Task WorkAsync(CancellationToken cancellationToken = default);
}
