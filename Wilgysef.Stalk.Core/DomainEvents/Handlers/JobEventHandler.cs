using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.DomainEvents.Events;
using Wilgysef.Stalk.Core.ItemIdSetServices;
using Wilgysef.Stalk.Core.JobScopeServices;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.IdGenerators;

namespace Wilgysef.Stalk.Core.DomainEvents.Handlers;

public class JobEventHandler :
    IDomainEventHandler<JobCreatedEvent>,
    IDomainEventHandler<JobStateChangedEvent>,
    IDomainEventHandler<JobPriorityChangedEvent>,
    IDomainEventHandler<JobDoneEvent>,
    ITransientDependency
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IJobScopeService _jobScopeService;
    private readonly IItemIdSetService _itemIdSetService;
    private readonly IIdGenerator<long> _idGenerator;

    public JobEventHandler(
        IBackgroundJobManager backgroundJobManager,
        IJobScopeService jobScopeService,
        IItemIdSetService itemIdSetService,
        IIdGenerator<long> idGenerator)
    {
        _backgroundJobManager = backgroundJobManager;
        _jobScopeService = jobScopeService;
        _itemIdSetService = itemIdSetService;
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

    public async Task HandleEventAsync(JobDoneEvent eventData, CancellationToken cancellationToken = default)
    {
        _jobScopeService.RemoveJobScope(eventData.JobId);
        await _itemIdSetService.RemoveItemIdSetAsync(eventData.JobId);
    }

    private async Task WorkPrioritizedJobsAsync(CancellationToken cancellationToken)
    {
        await _backgroundJobManager.EnqueueOrReplaceJobAsync(
            BackgroundJob.Create(
                _idGenerator.CreateId(),
                new WorkPrioritizedJobsArgs(),
                maximumAttempts: 2),
            false,
            cancellationToken);
    }
}
