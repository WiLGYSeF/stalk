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
            var jobs = await _jobManager.GetNextPriorityJobsAsync(_jobWorkerService.AvailableWorkers, cancellationToken);

            for (var i = 0; i < jobs.Count && _jobWorkerService.CanStartAdditionalWorkers; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _jobWorkerService.StartJobWorkerAsync(jobs[i]);
            }
            return;
        }

        var queuedJobs = await _jobManager.GetNextPriorityJobsAsync(_jobWorkerService.WorkerLimit, cancellationToken);
        var activeJobs = new Stack<Job>(_jobWorkerService.GetJobsByPriority());
        var queuedIndex = 0;

        while (activeJobs.Count > 0 && queuedIndex < queuedJobs.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var job = activeJobs.Pop();
            var queuedJob = queuedJobs[queuedIndex];
            if (job.Priority >= queuedJob.Priority)
            {
                break;
            }

            await _jobWorkerService.StopJobWorkerAsync(job);
            await _jobWorkerService.StartJobWorkerAsync(queuedJob);
            queuedIndex++;
        }
    }
}
