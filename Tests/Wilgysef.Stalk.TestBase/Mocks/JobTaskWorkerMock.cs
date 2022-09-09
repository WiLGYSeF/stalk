using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobTaskWorkerMock : JobTaskWorker
{
    public static int DelayInterval { get; } = 10;

    public event EventHandler WorkEvent;

    private bool _finishWork = false;
    private bool _throwException = false;

    public JobTaskWorkerMock(
        IServiceLifetimeScope lifetimeScope,
        HttpClient httpClient)
        : base(lifetimeScope, httpClient) { }

    public void Finish()
    {
        _finishWork = true;
    }

    public void Fail()
    {
        _throwException = true;
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
            if (_throwException)
            {
                throw new InvalidOperationException("Mock task failure.");
            }

            await Task.Delay(DelayInterval, cancellationToken);
        }
    }

    protected override async Task DownloadAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_finishWork)
        {
            if (_throwException)
            {
                throw new InvalidOperationException("Mock task failure.");
            }

            await Task.Delay(DelayInterval, cancellationToken);
        }
    }
}
