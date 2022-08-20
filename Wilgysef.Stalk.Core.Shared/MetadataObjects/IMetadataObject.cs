namespace Wilgysef.Stalk.Core.Shared.MetadataObjects
{
    public interface IMetadataObject
    {
        char KeySeparator { get; set; }

        object this[string key] { get; set; }

        void AddValue(string key, object value);

        bool TryAddValue(string key, object value);

        object GetValue(string key);

        bool TryGetValue(string key, out object value);

        bool ContainsValue(string key);

        bool RemoveValue(string key);
    }
}
