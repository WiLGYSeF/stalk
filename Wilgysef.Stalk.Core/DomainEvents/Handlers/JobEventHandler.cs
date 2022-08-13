using IdGen;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.DomainEvents.Events;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;

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

    public async Task HandleEventAsync(JobCreatedEvent eventData, CancellationToken cancellationToken = default)
    {
        await WorkPrioritizedJobs(cancellationToken);
    }

    public async Task HandleEventAsync(JobStateChangedEvent eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.NewState != JobState.Active)
        {
            await WorkPrioritizedJobs(cancellationToken);
        }
    }

    public async Task HandleEventAsync(JobPriorityChangedEvent eventData, CancellationToken cancellationToken = default)
    {
        await WorkPrioritizedJobs(cancellationToken);
    }

    private async Task WorkPrioritizedJobs(CancellationToken cancellationToken)
    {
        await _backgroundJobManager.EnqueueOrReplaceJobAsync(
            BackgroundJob.Create(
                _idGenerator.CreateId(),
                new WorkPrioritizedJobsArgs(),
                maximumLifespan: TimeSpan.FromSeconds(3)),
            false,
            cancellationToken);
    }
}
