using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobTaskWorkerMock : JobTaskWorker
{
    public static int DelayInterval { get; } = 10;

    public event EventHandler WorkEvent;

    private bool _finishWork = false;

    private readonly IServiceLifetimeScope _lifetimeScope;

    public JobTaskWorkerMock(IServiceLifetimeScope lifetimeScope)
        : base(lifetimeScope)
    {
        _lifetimeScope = lifetimeScope;
    }

    public void FinishWork()
    {
        _finishWork = true;
    }

    public override async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        WorkEvent?.Invoke(this, new EventArgs());

        await base.WorkAsync(cancellationToken);
    }

    protected override async Task ExtractAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_finishWork)
        {
            await Task.Delay(DelayInterval, cancellationToken);
        }
    }

    protected override async Task DownloadAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_finishWork)
        {
            await Task.Delay(DelayInterval, cancellationToken);
        }
    }
}
