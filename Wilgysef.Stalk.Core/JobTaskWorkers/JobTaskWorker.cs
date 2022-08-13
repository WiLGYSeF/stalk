using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobTaskWorker : IJobTaskWorker
{
    public JobTask? JobTask { get; private set; }

    private readonly IServiceLocator _serviceLocator;

    public JobTaskWorker(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public JobTaskWorker WithJobTask(JobTask task)
    {
        JobTask = task;
        return this;
    }

    public async Task WorkAsync(CancellationToken? cancellationToken = null)
    {

    }
}
