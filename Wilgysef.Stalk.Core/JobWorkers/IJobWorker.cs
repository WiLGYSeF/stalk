using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Interfaces;

namespace Wilgysef.Stalk.Core.JobWorkers;

public interface IJobWorker
{
    JobWorker WithJob(Job job);

    Task Work(CancellationToken? cancellationToken = null);
}
