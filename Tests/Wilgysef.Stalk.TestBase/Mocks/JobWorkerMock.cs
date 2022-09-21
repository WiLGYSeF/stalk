using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobWorkerMock : IJobWorker
{
    public Job Job { get; }

    public int WorkerLimit { get; set; } = 4;

    public TimeSpan TaskWaitTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

    private readonly IServiceLifetimeScope _lifetimeScope;

    public JobWorkerMock(
        IServiceLifetimeScope lifetimeScope,
        Job job)
    {
        _lifetimeScope = lifetimeScope;
        Job = job;
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
