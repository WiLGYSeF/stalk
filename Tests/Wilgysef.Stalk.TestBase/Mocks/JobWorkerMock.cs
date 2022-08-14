using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobWorkerMock : IJobWorker
{
    public Job Job { get; private set; } = null!;

    private readonly IServiceLocator _serviceLocator;

    public JobWorkerMock(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobWorker WithJob(Job job)
    {
        Job = job;
        return this;
    }

    public async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken);
        }
    }
}
