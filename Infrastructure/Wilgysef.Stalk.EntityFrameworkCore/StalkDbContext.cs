using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.DomainEvents;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.EntityFrameworkCore;

public class StalkDbContext : DbContext, IStalkDbContext
{
    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<JobTask> JobTasks { get; set; } = null!;

    public DbSet<BackgroundJob> BackgroundJobs { get; set; } = null!;

    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public StalkDbContext(
        DbContextOptions<StalkDbContext> options,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        // TODO: synchronous domain events?
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(cancellationToken);

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task DispatchDomainEvents(CancellationToken cancellationToken)
    {
        var domainEventEntities = ChangeTracker.Entries<IEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToArray();

        foreach (var entity in domainEventEntities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var events = entity.DomainEvents.ToArray();
            entity.DomainEvents.Clear();

            await _domainEventDispatcher.DispatchEvents(events, cancellationToken);
        }
    }
}
