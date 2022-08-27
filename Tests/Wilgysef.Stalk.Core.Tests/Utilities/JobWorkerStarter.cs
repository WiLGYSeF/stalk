using Shouldly;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.Tests.Utilities;

internal class JobWorkerStarter
{
    public int TaskWorkerLimit { get; set; } = 4;

    public TimeSpan TaskWaitTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

    private readonly IJobWorkerFactory _jobWorkerFactory;

    public JobWorkerStarter(IJobWorkerFactory jobWorkerFactory)
    {
        _jobWorkerFactory = jobWorkerFactory;
    }

    public JobWorkerInstance CreateAndStartWorker(Job job)
    {
        var worker = _jobWorkerFactory.CreateWorker(job);
        worker.WorkerLimit = TaskWorkerLimit;
        worker.TaskWaitTimeoutMilliseconds = (int)TaskWaitTimeout.TotalMilliseconds;

        var cancellationTokenSource = new CancellationTokenSource();
        var task = Task.Run(async () => await worker.WorkAsync(cancellationTokenSource.Token));
        return new JobWorkerInstance(worker, task, cancellationTokenSource);
    }

    public class JobWorkerInstance : IDisposable
    {
        public IJobWorker Worker { get; }

        public Task WorkerTask { get; }

        public CancellationTokenSource CancellationTokenSource { get; }

        public JobWorkerInstance(IJobWorker jobWorker, Task workerTask, CancellationTokenSource cancellationTokenSource)
        {
            Worker = jobWorker;
            WorkerTask = workerTask;
            CancellationTokenSource = cancellationTokenSource;
        }

        public void Dispose()
        {
            Worker.Job!.Tasks.All(t => t.Result.Success.GetValueOrDefault(true)).ShouldBeTrue();
            WorkerTask.Exception.ShouldBeNull();

            GC.SuppressFinalize(this);
        }
    }
}
