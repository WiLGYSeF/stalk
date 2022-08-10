﻿namespace Wilgysef.Stalk.Core.DomainEvents.Events;

public class JobCreatedEventHandler : IDomainEventHandler<JobCreatedEvent>
{
    public Task HandleEventAsync(JobCreatedEvent eventData)
    {
        return Task.CompletedTask;
    }
}
