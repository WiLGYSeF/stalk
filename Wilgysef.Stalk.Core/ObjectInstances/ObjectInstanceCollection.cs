namespace Wilgysef.Stalk.Core.ObjectInstances;

public class ObjectInstanceCollection<TKey, TValue> : IObjectInstanceCollection<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    public ICollection<TKey> Keys => _objects.Keys;

    public event EventHandler<TValue>? InstanceReleased;

    private readonly Dictionary<TKey, ObjectInstance> _objects = new();

    public IObjectInstanceHandle<TValue> GetHandle(TKey key, Func<TValue>? factory)
    {
        lock (_objects)
        {
            if (!_objects.TryGetValue(key, out var instance))
            {
                if (factory == null)
                {
                    throw new ArgumentNullException(nameof(factory), "The object instance does not exist and no factory was provided.");
                }

                instance = new ObjectInstance(this, key, factory());
                _objects[key] = instance;
            }
            return instance.GetHandle();
        }
    }

    protected bool Remove(TKey key, TValue value)
    {
        lock (_objects)
        {
            if (value is IDisposable disposable)
            {
                disposable?.Dispose();
            }

            InstanceReleased?.Invoke(this, value);
            return _objects.Remove(key);
        }
    }

    private class ObjectInstance
    {
        private readonly object _lock = new();

        private readonly ObjectInstanceCollection<TKey, TValue> _objectHandleCollection;
        private readonly TKey _key;
        private readonly TValue _value;
        private int _handleCount;

        public ObjectInstance(
            ObjectInstanceCollection<TKey, TValue> objectHandleCollection,
            TKey key,
            TValue value)
        {
            _objectHandleCollection = objectHandleCollection;
            _key = key;
            _value = value;
        }

        public ObjectInstanceHandle<TValue> GetHandle()
        {
            var handle = new ObjectInstanceHandle<TValue>(_value);

            lock (_lock)
            {
                _handleCount++;
                handle.Disposing += (_, _) => OnHandleDisposed();
            }

            return handle;
        }

        private void OnHandleDisposed()
        {
            lock (_lock)
            {
                if (--_handleCount == 0)
                {
                    _objectHandleCollection.Remove(_key, _value);
                }
            }
        }
    }
}
