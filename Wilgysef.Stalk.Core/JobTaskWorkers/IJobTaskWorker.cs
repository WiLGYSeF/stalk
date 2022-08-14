using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public interface IJobTaskWorker
{
    JobTaskWorker WithJobTask(JobTask job);

    Task WorkAsync(CancellationToken cancellationToken = default);
}
