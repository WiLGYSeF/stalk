using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.BackgroundJobs.Executors;

public class WorkPrioritizedJobsJob : IBackgroundJobHandler<WorkPrioritizedJobsArgs>
{
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerService _jobWorkerService;

    public WorkPrioritizedJobsJob(
        IJobManager jobManager,
        IJobWorkerService jobWorkerService)
    {
        _jobManager = jobManager;
        _jobWorkerService = jobWorkerService;
    }

    public async Task ExecuteJobAsync(WorkPrioritizedJobsArgs args)
    {
        if (_jobWorkerService.CanStartAdditionalWorkers)
        {
            while (_jobWorkerService.CanStartAdditionalWorkers)
            {
                var nextPriorityJob = await _jobManager.GetNextPriorityJobAsync();
                if (nextPriorityJob == null)
                {
                    break;
                }

                await _jobWorkerService.StartJobWorker(nextPriorityJob);
            }
            return;
        }

        var activeJobs = new Queue<Job>(_jobWorkerService.GetJobsByPriority());

        while (activeJobs.Count > 0)
        {
            var job = activeJobs.Dequeue();
            var nextPriorityJob = await _jobManager.GetNextPriorityJobAsync();
            if (nextPriorityJob == null || job.Priority >= nextPriorityJob.Priority)
            {
                break;
            }

            await _jobWorkerService.StopJobWorker(job);
            await _jobWorkerService.StartJobWorker(nextPriorityJob);
        }
    }
}
