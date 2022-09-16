using System;

namespace Wilgysef.Stalk.Core.Shared.CacheObjects
{
    public interface ICacheObject
    {
        object this[string key] { get; set; }

        void Add(string key, object value, DateTime? expires = null);

        bool TryAdd(string key, object value, DateTime? expires = null);

        void Set(string key, object value, DateTime? expires = null);

        bool TryGetValue(string key, out object value);

        bool ContainsKey(string key);

        bool Remove(string key);

        bool Remove(string key, out object value);

        void Clear();
    }
}
