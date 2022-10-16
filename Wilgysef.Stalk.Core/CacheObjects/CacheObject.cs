using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.Shared.CacheObjects;

namespace Wilgysef.Stalk.Core.CacheObjects;

public class CacheObject<TKey, TValue> : ICacheObject<TKey, TValue> where TKey : notnull
{
    public ICollection<TKey> Keys => _cache.Keys;

    public TValue? this[TKey key]
    {
        get => Get(key);
        set => Set(key, value);
    }

    private readonly ConcurrentDictionary<TKey, CacheValue<TValue>> _cache = new();

    public void Add(TKey key, TValue? value, DateTime? expires = null)
    {
        if (!TryAdd(key, value, expires))
        {
            throw new ArgumentException("Key already exists in cache.", nameof(key));
        }
    }

    public bool TryAdd(TKey key, TValue? value, DateTime? expires = null)
    {
        if (TryGetValue(key, out _))
        {
            return false;
        }

        _cache[key] = new CacheValue<TValue>(value, expires);
        return true;
    }

    public void Set(TKey key, TValue? value, DateTime? expires = null)
    {
        _cache[key] = new CacheValue<TValue>(value, expires);
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (_cache.TryGetValue(key, out var cacheValue) && !IsCacheValueExpired(cacheValue))
        {
            value = cacheValue.Value;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetValueAs<T>(TKey key, out T? value)
    {
        if (TryGetValue(key, out var val) && val is T castVal)
        {
            value = castVal;
            return true;
        }

        value = default;
        return false;
    }

    public bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out _);
    }

    public bool Remove(TKey key)
    {
        return TryGetValue(key, out _) && _cache.Remove(key, out _);
    }

    public bool Remove(TKey key, out TValue? value)
    {
        if (TryGetValue(key, out _) && _cache.Remove(key, out var cacheValue))
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

    public int RemoveExpired()
    {
        int removed = 0;
        foreach (var (key, cacheValue) in _cache)
        {
            if (IsCacheValueExpired(cacheValue))
            {
                Remove(key);
                removed++;
            }
        }
        return removed;
    }

    private TValue? Get(TKey key)
    {
        if (!TryGetValue(key, out var value))
        {
            throw new ArgumentException("Key does not exist in cache.", nameof(key));
        }

        return value;
    }

    private static bool IsCacheValueExpired(CacheValue<TValue> cacheValue)
    {
        return cacheValue.Expires.HasValue && cacheValue.Expires.Value < DateTime.Now;
    }

    private class CacheValue<T>
    {
        public T? Value { get; }

        public DateTime? Expires { get; }

        public CacheValue(T? value, DateTime? expires)
        {
            Value = value;
            Expires = expires;
        }
    }
}
