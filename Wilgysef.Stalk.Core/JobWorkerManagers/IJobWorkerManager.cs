using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Interfaces;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public interface IJobWorkerManager : ISingletonDependency
{
    bool StartJobWorker(Job job);
}
