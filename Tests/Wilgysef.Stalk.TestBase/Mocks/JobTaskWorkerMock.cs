using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobTaskWorkerMock : JobTaskWorker
{
    public static int DelayInterval { get; } = 10;

    public event EventHandler? WorkEvent;

    private bool _finishWork = false;
    private Exception? _exception = null;

    public JobTaskWorkerMock(
        IServiceLifetimeScope lifetimeScope,
        JobTask jobTask)
        : base(lifetimeScope, jobTask) { }

    public void Finish()
    {
        _finishWork = true;
    }

    public void Fail(Exception? exception = null)
    {
        _exception = exception ?? new Exception("Mock task fail.");
    }

    public override async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        WorkEvent?.Invoke(this, new EventArgs());

        await base.WorkAsync(cancellationToken);
    }

    protected override async Task ExtractAsync(CancellationToken cancellationToken)
    {
        await MockWork(cancellationToken);
    }

    protected override async Task DownloadAsync(CancellationToken cancellationToken)
    {
        await MockWork(cancellationToken);
    }

    private async Task MockWork(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_finishWork)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            await Task.Delay(DelayInterval, cancellationToken);
        }
    }
}
