using Wilgysef.Stalk.Core.DomainEvents.Events;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.DomainEvents.Handlers;

public class JobEventHandler :
    IDomainEventHandler<JobCreatedEvent>,
    IDomainEventHandler<JobStateChangedEvent>,
    IDomainEventHandler<JobPriorityChangedEvent>
{
    private readonly IJobWorkerService _jobWorkerService;
    private readonly IJobManager _jobManager;

    public JobEventHandler(
        IJobWorkerService jobWorkerService,
        IJobManager jobManager)
    {
        _jobWorkerService = jobWorkerService;
        _jobManager = jobManager;
    }

    public async Task HandleEventAsync(JobCreatedEvent eventData)
    {
        await WorkPrioritizedJobs();
    }

    public async Task HandleEventAsync(JobStateChangedEvent eventData)
    {
        await WorkPrioritizedJobs();
    }

    public async Task HandleEventAsync(JobPriorityChangedEvent eventData)
    {
        await WorkPrioritizedJobs();
    }

    private async Task WorkPrioritizedJobs()
    {
        // TODO: use background job
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

        var activeJobs = new Queue<Job>(_jobWorkerService.Jobs.OrderBy(j => j.Priority));

        while (activeJobs.Count > 0)
        {
            var job = activeJobs.Dequeue();
            var nextPriorityJob = await _jobManager.GetNextPriorityJobAsync();
            if (nextPriorityJob == null || job.Priority >= nextPriorityJob.Priority)
            {
                break;
            }
             
            // TODO: blocking
            await _jobWorkerService.StopJobWorker(job);
            await _jobWorkerService.StartJobWorker(nextPriorityJob);
        }
    }
}
