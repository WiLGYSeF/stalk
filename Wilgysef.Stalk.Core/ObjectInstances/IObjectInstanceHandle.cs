namespace Wilgysef.Stalk.Core.ObjectInstances;

/// <summary>
/// Do not dispose the value in <see cref="IDisposable.Dispose()"/>!
/// </summary>
/// <typeparam name="T">Instance type.</typeparam>
public interface IObjectInstanceHandle<T> : IDisposable where T : notnull
{
    /// <summary>
    /// Instance value.
    /// </summary>
    T Value { get; }
}
