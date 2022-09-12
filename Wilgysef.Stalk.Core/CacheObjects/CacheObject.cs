using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.Shared.CacheObjects;

namespace Wilgysef.Stalk.Core.CacheObjects;

public class CacheObject : ICacheObject
{
    public ICollection<string> Keys => _cache.Keys;

    public ICollection<object?> Values => _cache.Values;

    public int Count => _cache.Count;

    public object? this[string key]
    {
        get => _cache[key];
        set => _cache[key] = value;
    }

    private readonly ConcurrentDictionary<string, object?> _cache = new();

    public void Add(string key, object? value)
    {
        if (!_cache.TryAdd(key, value))
        {
            throw new ArgumentException("Key already exists in cache.", nameof(key));
        }
    }

    public bool TryAdd(string key, object? value)
    {
        return _cache.TryAdd(key, value);
    }

    public bool TryGetValue(string key, out object? value)
    {
        return _cache.TryGetValue(key, out value);
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
        return _cache.Remove(key, out value);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
