using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public interface IJobTaskWorker
{
    public Job? Job { get; }

    public JobTask? JobTask { get; }

    JobTaskWorker WithJobTask(Job job, JobTask jobTask);

    Task WorkAsync(CancellationToken cancellationToken = default);
}
