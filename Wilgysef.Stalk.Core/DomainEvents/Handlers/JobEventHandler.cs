using IdGen;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.DomainEvents.Events;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.DomainEvents.Handlers;

public class JobEventHandler :
    IDomainEventHandler<JobCreatedEvent>,
    IDomainEventHandler<JobStateChangedEvent>,
    IDomainEventHandler<JobPriorityChangedEvent>
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobEventHandler(
        IBackgroundJobManager backgroundJobManager,
        IIdGenerator<long> idGenerator)
    {
        _backgroundJobManager = backgroundJobManager;
        _idGenerator = idGenerator;
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
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                _idGenerator.CreateId(),
                new WorkPrioritizedJobsArgs()),
            false);
    }
}
