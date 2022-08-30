using System.Diagnostics.CodeAnalysis;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.MetadataObjects;

public class MetadataObject : IMetadataObject
{
    public char KeySeparator { get; set; }

    public IDictionary<string, object> Dictionary => _dictionary;

    public bool HasValues => _dictionary.Count > 0;

    public MetadataObject(char keySeparator)
    {
        KeySeparator = keySeparator;
    }

    public MetadataObject(IDictionary<string, object> dictionary, char keySeparator) : this(keySeparator)
    {
        foreach (var pair in dictionary)
        {
            this[pair.Key] = pair.Value;
        }
    }

    public object this[string key]
    {
        get => GetValue(key);
        set => SetValue(key, value, false);
    }

    private readonly Dictionary<string, object> _dictionary = new();

    public void AddValue(string key, object value)
    {
        SetValue(key, value, true);
    }

    public bool TryAddValue(string key, object value)
    {
        try
        {
            SetValue(key, value, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public object GetValue(string key)
    {
        return TryGetValue(key, out var value)
            ? value
            : throw new ArgumentException("Could not get value by key.");
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    {
        if (!TryGetPenultimateDictionary(key, out var dictionary, out var ultimateKey))
        {
            value = default;
            return false;
        }

        if (!dictionary.TryGetValue(ultimateKey, out value))
        {
            value = default;
            return false;
        }
        return true;
    }

    public bool ContainsValue(string key)
    {
        return TryGetValue(key, out _);
    }

    public bool RemoveValue(string key)
    {
        if (!TryGetPenultimateDictionary(key, out var dictionary, out var ultimateKey))
        {
            return false;
        }
        return dictionary.Remove(ultimateKey);
    }

    private void SetValue(string key, object value, bool throwIfExisting)
    {
        if (!TryGetPenultimateDictionary(key, out var dictionary, out var ultimateKey))
        {
            dictionary = CreateDictionaries(key, out ultimateKey);
        }

        if (throwIfExisting && dictionary.ContainsKey(ultimateKey))
        {
            throw new ArgumentException("Key already exists", nameof(key));
        }

        dictionary[ultimateKey] = value;
    }

    private bool TryGetPenultimateDictionary(
        string key,
        [MaybeNullWhen(false)] out IDictionary<string, object> dictionary,
        [MaybeNullWhen(false)] out string ultimateKey)
    {
        var keyParts = GetKeyParts(key);
        IDictionary<string, object> dict = _dictionary;

        for (var i = 0; i < keyParts.Length - 1; i++)
        {
            if (!TryGetValueAsDictionary(dict, keyParts[i], out var value))
            {
                dictionary = default;
                ultimateKey = default;
                return false;
            }
            dict = value;
        }

        dictionary = dict;
        ultimateKey = keyParts[^1];
        return true;
    }

    private string[] GetKeyParts(string key)
    {
        return key.Split(KeySeparator);
    }

    private IDictionary<string, object> CreateDictionaries(string key, out string ultimateKey)
    {
        var keyParts = GetKeyParts(key);
        var dictionary = (IDictionary<string, object>)_dictionary;

        for (var i = 0; i < keyParts.Length - 1; i++)
        {
            var keyPart = keyParts[i];
            if (!TryGetValueAsDictionary(dictionary, keyPart, out var dict))
            {
                if (dictionary.ContainsKey(keyPart))
                {
                    throw new ArgumentException("The key value is not a dictionary.", nameof(key));
                }
                dict = new Dictionary<string, object>();
                dictionary[keyPart] = dict;
            }
            dictionary = dict;
        }

        ultimateKey = keyParts[^1];
        return dictionary;
    }

    private static bool TryGetValueAsDictionary(
        IDictionary<string, object> dictionary,
        string key,
        [MaybeNullWhen(false)] out IDictionary<string, object> value)
    {
        if (!dictionary.TryGetValue(key, out var objectValue))
        {
            value = default;
            return false;
        }
        value = objectValue as IDictionary<string, object>;
        return value != null;
    }

}
