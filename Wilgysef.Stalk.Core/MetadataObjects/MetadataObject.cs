using System.Diagnostics.CodeAnalysis;
using System.Text;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Tries;

namespace Wilgysef.Stalk.Core.MetadataObjects;

public class MetadataObject : IMetadataObject
{
    public char KeySeparator { get; set; }

    public bool HasValues => _root.Count > 0;

    private readonly Trie<string, object?> _root = new(null!, null);

    #if DEBUG

    private IDictionary<string, object?> Dictionary => GetDictionary();

    #endif

    public object? this[string key]
    {
        get => GetValue(key);
        set => SetByParts(value, GetKeyParts(key));
    }

    public MetadataObject(char keySeparator)
    {
        KeySeparator = keySeparator;
    }

    public void Add(string key, object? value)
    {
        AddByParts(value, GetKeyParts(key));
    }

    public void AddByParts(object? value, params string[] keyParts)
    {
        SetValue(value, true, keyParts);
    }

    public bool TryAddValue(string key, object? value)
    {
        return TryAddValueByParts(value, GetKeyParts(key));
    }

    public bool TryAddValueByParts(object? value, params string[] keyParts)
    {
        try
        {
            SetValue(value, true, keyParts);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SetByParts(object? value, params string[] keyParts)
    {
        SetValue(value, false, keyParts);
    }

    public object? GetValue(string key)
    {
        return GetValueByParts(GetKeyParts(key));
    }

    public object? GetValueByParts(params string[] keyParts)
    {
        return TryGetValueByParts(out var value, keyParts)
            ? value
            : throw new ArgumentException("Could not get value by key.", nameof(keyParts));
    }

    public bool TryGetValue(string key, out object? value)
    {
        return TryGetValueByParts(out value, GetKeyParts(key));
    }

    public bool TryGetValueByParts(out object? value, params string[] keyParts)
    {
        if (!TryGetPenultimateTrie(out var trie, out var ultimateKey, keyParts))
        {
            value = default;
            return false;
        }

        if (!trie.TryGetChild(ultimateKey, out var leafTrie))
        {
            value = default;
            return false;
        }
        value = leafTrie.Value;
        return true;
    }

    public bool Contains(string key)
    {
        return ContainsByParts(GetKeyParts(key));
    }

    public bool ContainsByParts(params string[] keyParts)
    {
        return TryGetValueByParts(out _, keyParts);
    }

    public bool Remove(string key)
    {
        return RemoveByParts(GetKeyParts(key));
    }

    public bool RemoveByParts(params string[] keyParts)
    {
        if (!TryGetPenultimateTrie(out var trie, out var ultimateKey, keyParts))
        {
            return false;
        }
        return trie.Remove(ultimateKey);
    }

    public IMetadataObject Copy()
    {
        var metadata = new MetadataObject(KeySeparator);
        var nodes = new Queue<(string Key, ITrie<string, object?> Trie)>(_root.Children.Select(t => (t.Key, t)));

        while (nodes.Count > 0)
        {
            var (key, trie) = nodes.Dequeue();

            if (trie.Count > 0)
            {
                foreach (var child in trie.Children)
                {
                    nodes.Enqueue((key + KeySeparator + child.Key, child));
                }
            }
            else
            {
                metadata[key] = trie.Value;
            }
        }

        return metadata;
    }

    public IDictionary<string, object?> GetDictionary()
    {
        IDictionary<string, object?> dictionary = new Dictionary<string, object?>();
        var nodes = new Queue<(ITrie<string, object?> Trie, IDictionary<string, object?> Dict)>(_root.Children.Select(t => (t, dictionary)));

        while (nodes.Count > 0)
        {
            var (trie, dict) = nodes.Dequeue();

            if (trie.Count > 0)
            {
                IDictionary<string, object?> newDict = new Dictionary<string, object?>();
                dict[trie.Key] = newDict;

                foreach (var child in trie.Children)
                {
                    nodes.Enqueue((child, newDict));
                }
            }
            else
            {
                dict[trie.Key] = trie.Value;
            }
        }

        return dictionary;
    }

    public IDictionary<string, object?> GetFlattenedDictionary()
    {
        IDictionary<string, object?> dictionary = new Dictionary<string, object?>();
        var nodes = new Queue<(ITrie<string, object?> Trie, string Key)>(_root.Children.Select(t => (t, t.Key)));

        while (nodes.Count > 0)
        {
            var (trie, key) = nodes.Dequeue();

            if (trie.Count > 0)
            {
                foreach (var child in trie.Children)
                {
                    nodes.Enqueue((child, key + KeySeparator + child.Key));
                }
            }
            else
            {
                dictionary[key] = trie.Value;
            }
        }

        return dictionary;
    }

    public void From(IDictionary<object, object> dictionary)
    {
        SetValues(dictionary, null);
    }

    public string GetKey(params string[] keyParts)
    {
        return string.Join(KeySeparator, keyParts);
    }

    private void SetValue(object? value, bool throwIfExisting, params string[] keyParts)
    {
        if (!TryGetPenultimateTrie(out var trie, out var ultimateKey, keyParts))
        {
            trie = CreateTries(out ultimateKey, keyParts);
        }
        if (trie.Terminal)
        {
            throw new ArgumentException("Subkey already exists", nameof(keyParts));
        }
        if (throwIfExisting && trie.Contains(ultimateKey))
        {
            throw new ArgumentException("Key already exists", nameof(keyParts));
        }

        var newTrie = new Trie<string, object?>(ultimateKey, value)
        {
            Terminal = true
        };
        trie[ultimateKey] = newTrie;
    }

    private bool TryGetPenultimateTrie(
        [MaybeNullWhen(false)] out ITrie<string, object?> trie,
        [MaybeNullWhen(false)] out string ultimateKey,
        params string[] keyParts)
    {
        ITrie<string, object?> currentTrie = _root;

        for (var i = 0; i < keyParts.Length - 1; i++)
        {
            if (!currentTrie.TryGetChild(keyParts[i], out var value))
            {
                trie = default;
                ultimateKey = default;
                return false;
            }
            currentTrie = value;
        }

        trie = currentTrie;
        ultimateKey = keyParts[^1];
        return true;
    }

    private ITrie<string, object?> CreateTries(out string ultimateKey, params string[] keyParts)
    {
        ITrie<string, object?> currentTrie = _root;

        for (var i = 0; i < keyParts.Length - 1; i++)
        {
            var keyPart = keyParts[i];
            if (!currentTrie.TryGetChild(keyPart, out var trie))
            {
                trie = new Trie<string, object?>(keyPart, null);
                currentTrie[keyPart] = trie;
            }
            else if (trie.Terminal)
            {
                throw new ArgumentException("Subkey already exists", nameof(keyParts));
            }
            currentTrie = trie;
        }

        ultimateKey = keyParts[^1];
        return currentTrie;
    }

    private void SetValues(IDictionary<object, object> dictionary, string? root)
    {
        foreach (var (key, value) in dictionary)
        {
            var keyToString = key.ToString();
            if (keyToString == null)
            {
                continue;
            }

            var keyStr = root != null ? root + KeySeparator + keyToString : keyToString;
            if (value is IDictionary<object, object> dict)
            {
                SetValues(dict, keyStr);
            }
            else
            {
                this[keyStr] = value;
            }
        }
    }

    private string[] GetKeyParts(string key)
    {
        return key.Split(KeySeparator);
    }
}
