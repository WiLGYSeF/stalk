namespace Wilgysef.Stalk.Core.ObjectInstances;

public class ObjectInstanceHandle<T> : IObjectInstanceHandle<T> where T : notnull
{
    public T Value { get; }

    public event EventHandler? Disposing;

    public ObjectInstanceHandle(T value)
    {
        Value = value;
    }

    public void Dispose()
    {
        Disposing?.Invoke(this, EventArgs.Empty);

        GC.SuppressFinalize(this);
    }
}
