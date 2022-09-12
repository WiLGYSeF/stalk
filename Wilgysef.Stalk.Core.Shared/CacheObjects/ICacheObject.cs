namespace Wilgysef.Stalk.Core.Shared.CacheObjects
{
    public interface ICacheObject
    {
        void Add(string key, object value);

        bool TryAdd(string key, object value);

        bool TryGetValue(string key, out object value);

        bool ContainsKey(string key);

        bool Remove(string key);

        bool Remove(string key, out object value);

        void Clear();
    }
}
