using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public class JobWorkerManager : IJobWorkerManager
{
    public IReadOnlyCollection<JobWorker> Workers => _jobWorkers;

    private int WorkerLimit { get; set; } = 4;

    private readonly List<JobWorker> _jobWorkers = new();
    private readonly Dictionary<JobWorker, CancellationTokenSource> _jobWorkerCancellationTokenSources = new();

    private readonly IJobWorkerFactory _jobWorkerFactory;

    public JobWorkerManager(
        IJobWorkerFactory jobWorkerFactory)
    {
        _jobWorkerFactory = jobWorkerFactory;
    }

    public bool StartJobWorker(Job job)
    {
        if (_jobWorkers.Count >= WorkerLimit)
        {
            return false;
        }

        var worker = _jobWorkerFactory.CreateWorker(job);
        var cancellationTokenSource = new CancellationTokenSource();

        _jobWorkers.Add(worker);
        _jobWorkerCancellationTokenSources.Add(worker, cancellationTokenSource);

        // do not await
        worker.Work(cancellationTokenSource.Token);

        return true;
    }
}
