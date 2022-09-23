using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public interface IJobTaskWorker
{
    JobTask JobTask { get; }

    Task WorkAsync(CancellationToken cancellationToken = default);
}
