using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public interface IJobTaskWorker : IDisposable
{
    public JobTask? JobTask { get; }

    IJobTaskWorker WithJobTask(JobTask jobTask);

    Task WorkAsync(CancellationToken cancellationToken = default);
}
