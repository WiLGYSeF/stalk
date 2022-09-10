using System.Diagnostics.CodeAnalysis;
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
        set => SetValue(key, value, false);
    }

    public MetadataObject(char keySeparator)
    {
        KeySeparator = keySeparator;
    }

    public void AddValue(string key, object? value)
    {
        SetValue(key, value, true);
    }

    public bool TryAddValue(string key, object? value)
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

    public object? GetValue(string key)
    {
        return TryGetValue(key, out var value)
            ? value
            : throw new ArgumentException("Could not get value by key.", nameof(key));
    }

    public bool TryGetValue(string key, out object? value)
    {
        if (!TryGetPenultimateTrie(key, out var trie, out var ultimateKey))
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

    public bool ContainsValue(string key)
    {
        return TryGetValue(key, out _);
    }

    public bool RemoveValue(string key)
    {
        if (!TryGetPenultimateTrie(key, out var trie, out var ultimateKey))
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

    public void From(IDictionary<object, object> dictionary)
    {
        SetValues(dictionary, null);
    }

    private void SetValue(string key, object? value, bool throwIfExisting)
    {
        if (!TryGetPenultimateTrie(key, out var trie, out var ultimateKey))
        {
            trie = CreateTries(key, out ultimateKey);
        }
        if (trie.Terminal)
        {
            throw new ArgumentException("Subkey already exists", nameof(key));
        }
        if (throwIfExisting && trie.Contains(ultimateKey))
        {
            throw new ArgumentException("Key already exists", nameof(key));
        }

        var newTrie = new Trie<string, object?>(ultimateKey, value)
        {
            Terminal = true
        };
        trie[ultimateKey] = newTrie;
    }

    private bool TryGetPenultimateTrie(
        string key,
        [MaybeNullWhen(false)] out ITrie<string, object?> trie,
        [MaybeNullWhen(false)] out string ultimateKey)
    {
        var keyParts = GetKeyParts(key);
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

    private ITrie<string, object?> CreateTries(string key, out string ultimateKey)
    {
        var keyParts = GetKeyParts(key);
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
                throw new ArgumentException("Subkey already exists", nameof(key));
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
