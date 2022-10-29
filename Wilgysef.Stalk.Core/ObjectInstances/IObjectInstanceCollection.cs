namespace Wilgysef.Stalk.Core.ObjectInstances;

/// <summary>
/// A collection of object instances.
/// Store instances of objects that will be released/disposed when there are no more references.
/// </summary>
/// <typeparam name="TKey">Instance key type.</typeparam>
/// <typeparam name="TValue">Instance type.</typeparam>
public interface IObjectInstanceCollection<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    /// <summary>
    /// Instance keys.
    /// </summary>
    ICollection<TKey> Keys { get; }

    /// <summary>
    /// Invoked when an instance is released.
    /// </summary>
    event EventHandler<TValue>? InstanceReleased;

    /// <summary>
    /// Gets an instance handle for the object instance.
    /// </summary>
    /// <param name="key">Instance key.</param>
    /// <param name="factory">Instance factory, only called if no instance exists for <paramref name="key"/>.</param>
    /// <returns>Instance handle.</returns>
    IObjectInstanceHandle<TValue> GetHandle(TKey key, Func<TValue>? factory);
}
