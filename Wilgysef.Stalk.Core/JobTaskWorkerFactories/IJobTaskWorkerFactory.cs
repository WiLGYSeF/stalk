using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public interface IJobTaskWorkerFactory : ITransientDependency
{
    JobTaskWorker CreateWorker(JobTask task);
}
