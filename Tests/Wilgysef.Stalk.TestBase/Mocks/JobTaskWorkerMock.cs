using System.Diagnostics;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobTaskWorkerMock : IJobTaskWorker
{
    public Job? Job { get; private set; }

    public JobTask? JobTask { get; private set; }

    public event EventHandler WorkEvent;

    private readonly IServiceLocator _serviceLocator;

    public JobTaskWorkerMock(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobTaskWorker WithJobTask(Job job, JobTask jobTask)
    {
        Job = job;
        JobTask = jobTask;
        return this;
    }

    public async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        WorkEvent(this, new EventArgs());

        while (!cancellationToken.IsCancellationRequested)
        {
            Debug.WriteLine($"a {Thread.CurrentThread.ManagedThreadId}");
            await Task.Delay(100, cancellationToken);
        }
    }
}
