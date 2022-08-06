using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public class JobTaskWorkerFactory : IJobTaskWorkerFactory
{
    private readonly IJobManager _jobManager;

    public JobTaskWorkerFactory(
        IJobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public JobTaskWorker CreateWorker(JobTask task)
    {
        // TODO: this probably needs to be a service locator since we need to drop the db context when not in use
        var taskWorker = new JobTaskWorker(_jobManager);
        taskWorker.WithJobTask(task);
        return taskWorker;
    }
}
