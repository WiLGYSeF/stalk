using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public interface IJobTaskWorker : IDisposable
{
    JobTask JobTask { get; }

    Task WorkAsync(CancellationToken cancellationToken = default);
}
