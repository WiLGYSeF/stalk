using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.BackgroundJobs.Executors;

public class WorkPrioritizedJobsJob : BackgroundJobHandler<WorkPrioritizedJobsArgs>
{
    public ILogger? Logger { get; set; }

    private readonly IJobManager _jobManager;
    private readonly IJobWorkerService _jobWorkerService;

    public WorkPrioritizedJobsJob(
        IJobManager jobManager,
        IJobWorkerService jobWorkerService)
    {
        _jobManager = jobManager;
        _jobWorkerService = jobWorkerService;
    }

    public override async Task ExecuteJobAsync(WorkPrioritizedJobsArgs args, CancellationToken cancellationToken = default)
    {
        if (_jobWorkerService.CanStartAdditionalWorkers)
        {
            while (_jobWorkerService.CanStartAdditionalWorkers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var nextPriorityJob = await _jobManager.GetNextPriorityJobAsync(cancellationToken);
                if (nextPriorityJob == null)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await _jobWorkerService.StartJobWorkerAsync(nextPriorityJob);
            }
            return;
        }

        var activeJobs = new Stack<Job>(_jobWorkerService.GetJobsByPriority());

        while (activeJobs.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var job = activeJobs.Pop();
            var nextPriorityJob = await _jobManager.GetNextPriorityJobAsync(cancellationToken);
            if (nextPriorityJob == null || job.Priority >= nextPriorityJob.Priority)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _jobWorkerService.StopJobWorkerAsync(job);
            await _jobWorkerService.StartJobWorkerAsync(nextPriorityJob);
        }
    }
}
