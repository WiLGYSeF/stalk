using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobWorkers;

public interface IJobTaskWorker
{
    JobTaskWorker WithJobTask(JobTask job);

    Task WorkAsync(CancellationToken? cancellationToken = null);
}
