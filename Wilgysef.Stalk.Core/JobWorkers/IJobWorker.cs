using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkers;

public interface IJobWorker : IDisposable
{
    Job Job { get; }

    int WorkerLimit { get; set; }

    TimeSpan TaskWaitTimeout { get; set; }

    Task WorkAsync(CancellationToken cancellationToken = default);
}
