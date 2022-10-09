using System;
using System.Collections.Generic;

namespace Wilgysef.Stalk.Core.Shared.CacheObjects
{
    public interface ICacheObject
    {
        ICollection<object> Keys { get; }

        object? this[object key] { get; set; }

        void Add(object key, object? value, DateTime? expires = null);

        bool TryAdd(object key, object? value, DateTime? expires = null);

        void Set(object key, object? value, DateTime? expires = null);

        bool TryGetValue(object key, out object? value);

        bool TryGetValueAs<T>(object key, out T value);

        bool ContainsKey(object key);

        bool Remove(object key);

        bool Remove(object key, out object? value);

        void Clear();

        int RemoveExpired();
    }

    public interface ICacheObject<TKey, TValue>
    {
        ICollection<TKey> Keys { get; }

        TValue this[TKey key] { get; set; }

        void Add(TKey key, TValue value, DateTime? expires = null);

        bool TryAdd(TKey key, TValue value, DateTime? expires = null);

        void Set(TKey key, TValue value, DateTime? expires = null);

        bool TryGetValue(TKey key, out TValue value);

        bool TryGetValueAs<T>(TKey key, out T value);

        bool ContainsKey(TKey key);

        bool Remove(TKey key);

        bool Remove(TKey key, out TValue value);

        void Clear();

        int RemoveExpired();
    }
}
