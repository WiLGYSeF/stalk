using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.DomainEvents.Events;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.IdGenerators;

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
        await WorkPrioritizedJobsAsync(cancellationToken);
    }

    public async Task HandleEventAsync(JobStateChangedEvent eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.NewState != JobState.Active)
        {
            await WorkPrioritizedJobsAsync(cancellationToken);
        }
    }

    public async Task HandleEventAsync(JobPriorityChangedEvent eventData, CancellationToken cancellationToken = default)
    {
        await WorkPrioritizedJobsAsync(cancellationToken);
    }

    private async Task WorkPrioritizedJobsAsync(CancellationToken cancellationToken)
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
