using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public interface IJobWorkerFactory : ITransientDependency
{
    IJobWorker CreateWorker(Job job);
}
