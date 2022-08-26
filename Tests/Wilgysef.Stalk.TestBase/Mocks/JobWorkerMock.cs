using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobWorkerMock : IJobWorker
{
    public Job Job { get; private set; } = null!;

    public int TaskWaitTimeoutMilliseconds { get; set; } = 500;

    private readonly IServiceLifetimeScope _lifetimeScope;

    public JobWorkerMock(IServiceLifetimeScope lifetimeScope)
    {
        _lifetimeScope = lifetimeScope;
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

    public void Dispose()
    {
        _lifetimeScope.Dispose();

        GC.SuppressFinalize(this);
    }
}
