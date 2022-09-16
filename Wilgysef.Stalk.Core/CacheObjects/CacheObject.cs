using Wilgysef.Stalk.Core.Shared.CacheObjects;

namespace Wilgysef.Stalk.Core.CacheObjects;

public class CacheObject : ICacheObject
{
    public ICollection<string> Keys => _cache.Keys;

    public ICollection<object?> Values => _cache.Values.Select(v => v.Value).ToList();

    public int Count => _cache.Count;

    public object? this[string key]
    {
        get => Get(key);
        set => Set(key, value);
    }

    private readonly Dictionary<string, CacheValue> _cache = new();
    private readonly object _lock = new();

    public void Add(string key, object? value, DateTime? expires = null)
    {
        if (!TryAdd(key, value, expires))
        {
            throw new ArgumentException("Key already exists in cache.", nameof(key));
        }
    }

    public bool TryAdd(string key, object? value, DateTime? expires = null)
    {
        return _cache.TryAdd(key, new CacheValue(value, expires));
    }

    public void Set(string key, object? value, DateTime? expires = null)
    {
        _cache[key] = new CacheValue(value, expires);
    }

    public bool TryGetValue(string key, out object? value)
    {
        if (_cache.TryGetValue(key, out var cacheValue))
        {
            value = cacheValue.Value;
            return true;
        }
        value = default;
        return false;
    }

    public bool ContainsKey(string key)
    {
        return _cache.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        return _cache.Remove(key, out _);
    }

    public bool Remove(string key, out object? value)
    {
        if (_cache.Remove(key, out var cacheValue))
        {
            value = cacheValue.Value;
            return true;
        }
        value = default;
        return false;
    }

    public void Clear()
    {
        _cache.Clear();
    }

    private object? Get(string key)
    {
        return _cache[key];
    }

    private class CacheValue
    {
        public object? Value { get; set; }

        public DateTime? Expires { get; set; }

        public CacheValue(object? value, DateTime? expires)
        {
            Value = value;
            Expires = expires;
        }
    }
}
