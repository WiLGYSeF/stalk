using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public class JobWorkerManager : IJobWorkerManager
{
    public IReadOnlyCollection<JobWorker> Workers => _jobWorkers;

    private int WorkerLimit { get; set; } = 4;

    private readonly List<JobWorker> _jobWorkers = new();

    private readonly IJobWorkerFactory _jobWorkerFactory;

    public JobWorkerManager(
        IJobWorkerFactory jobWorkerFactory)
    {
        _jobWorkerFactory = jobWorkerFactory;
    }

    public bool StartJob(Job job)
    {
        if (_jobWorkers.Count >= WorkerLimit)
        {
            return false;
        }

        var worker = _jobWorkerFactory.CreateWorker(job);
        _jobWorkers.Add(worker);

        // do not await
        worker.Work();

        return true;
    }
}
