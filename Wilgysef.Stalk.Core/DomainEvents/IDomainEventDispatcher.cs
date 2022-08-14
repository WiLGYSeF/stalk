﻿using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.DomainEvents;

public interface IDomainEventDispatcher : ITransientDependency
{
    /// <summary>
    /// Dispatch events.
    /// </summary>
    /// <typeparam name="T">Event type.</typeparam>
    /// <param name="eventData">Event data.</param>
    /// <returns></returns>
    Task DispatchEvents<T>(params T[] eventData) where T : notnull, IDomainEvent;

    /// <summary>
    /// Dispatch events.
    /// </summary>
    /// <typeparam name="T">Event type.</typeparam>
    /// <param name="eventData">Event data.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DispatchEvents<T>(IEnumerable<T> eventData, CancellationToken cancellationToken = default) where T : notnull, IDomainEvent;
}
