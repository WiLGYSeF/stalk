﻿using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.DomainEvents;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.EntityFrameworkCore;

public class StalkDbContext : DbContext, IStalkDbContext, IScopedDependency
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
        throw new NotImplementedException("Use SaveChangesAsync() instead.");
        //return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
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

            await _domainEventDispatcher.DispatchEventsAsync(events, cancellationToken);
        }
    }
}
