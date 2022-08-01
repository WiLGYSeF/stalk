using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Interfaces;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public interface IJobWorkerFactory : ITransientDependency
{
    JobWorker CreateWorker(Job job);
}
