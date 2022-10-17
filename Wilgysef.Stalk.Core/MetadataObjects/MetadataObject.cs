using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Tries;

namespace Wilgysef.Stalk.Core.MetadataObjects;

[DebuggerTypeProxy(typeof(MetadataDebugView))]
public class MetadataObject : IMetadataObject
{
    public bool HasValues => _root.Count > 0;

    private readonly Trie<string, object?> _root = new(null!, null);

    public object? this[params string[] keys]
    {
        get => GetValue(keys);
        set => SetValue(value, false, keys);
    }

    public void Add(object? value, params string[] keys)
    {
        Add(value, (IEnumerable<string>)keys);
    }

    public void Add(object? value, IEnumerable<string> keys)
    {
        SetValue(value, true, keys);
    }

    public bool TryAddValue(object? value, params string[] keys)
    {
        return TryAddValue(value, (IEnumerable<string>)keys);
    }

    public bool TryAddValue(object? value, IEnumerable<string> keys)
    {
        try
        {
            SetValue(value, true, keys);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public object? GetValue(params string[] keys)
    {
        return GetValue((IEnumerable<string>)keys);
    }

    public object? GetValue(IEnumerable<string> keys)
    {
        return TryGetValue(out var value, keys)
            ? value
            : throw new ArgumentException("Could not get value by key.", nameof(keys));
    }

    public T? GetValueAs<T>(params string[] keys)
    {
        return GetValueAs<T>((IEnumerable<string>)keys);
    }

    public T? GetValueAs<T>(IEnumerable<string> keys)
    {
        return TryGetValueAs<T>(out var value, keys)
            ? value
            : throw new ArgumentException("Could not get value by key.", nameof(keys));
    }

    public bool TryGetValue(out object? value, params string[] keys)
    {
        return TryGetValue(out value, (IEnumerable<string>)keys);
    }

    public bool TryGetValue(out object? value, IEnumerable<string> keys)
    {
        if (!TryGetPenultimateTrie(out var trie, out var ultimateKey, keys)
            || !trie.TryGetChild(ultimateKey, out var leafTrie))
        {
            value = default;
            return false;
        }

        value = leafTrie.Value;
        return true;
    }

    public bool TryGetValueAs<T>(out T? value, params string[] keys)
    {
        return TryGetValueAs<T>(out value, (IEnumerable<string>)keys);
    }

    public bool TryGetValueAs<T>(out T? value, IEnumerable<string> keys)
    {
        if (!TryGetPenultimateTrie(out var trie, out var ultimateKey, keys)
            || !trie.TryGetChild(ultimateKey, out var leafTrie))
        {
            value = default;
            return false;
        }

        if (leafTrie.Value is T val)
        {
            value = val;
            return true;
        }

        value = default;
        return false;
    }

    public bool Contains(params string[] keys)
    {
        return Contains((IEnumerable<string>)keys);
    }

    public bool Contains(IEnumerable<string> keys)
    {
        return TryGetValue(out _, keys);
    }

    public bool Remove(params string[] keys)
    {
        return Remove((IEnumerable<string>)keys);
    }

    public bool Remove(IEnumerable<string> keys)
    {
        if (!TryGetPenultimateTrie(out var trie, out var ultimateKey, keys))
        {
            return false;
        }
        return trie.Remove(ultimateKey);
    }

    public void Clear()
    {
        _root.Clear();
    }

    public IMetadataObject Copy()
    {
        var metadata = new MetadataObject();
        GetValues((keys, value) => metadata.SetValue(value, false, keys.ToArray()));
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

    public IDictionary<string, object?> GetFlattenedDictionary(string separator)
    {
        IDictionary<string, object?> dictionary = new Dictionary<string, object?>();
        GetValues((keys, value) => dictionary[string.Join(separator, keys)] = value);
        return dictionary;
    }

    public void From(IDictionary<object, object?> dictionary)
    {
        GetValues(
            dictionary,
            p => p.Key.ToString()!,
            p => p.Value,
            p => (IDictionary<object, object?>)p.Value!,
            p => (p.Value as IDictionary<object, object?>)?.Count > 0,
            (keys, value) => SetValue(value, false, keys));
    }

    public void From(IDictionary<string, object?> dictionary)
    {
        GetValues(
            dictionary,
            p => p.Key,
            p => p.Value,
            p => (IDictionary<string, object?>)p.Value!,
            p => (p.Value as IDictionary<string, object?>)?.Count > 0,
            (keys, value) => SetValue(value, false, keys));
    }

    private void SetValue(object? value, bool throwIfExisting, IEnumerable<string> keys)
    {
        if (!TryGetPenultimateTrie(out var trie, out var ultimateKey, keys))
        {
            trie = CreateTries(out ultimateKey, keys);
        }
        if (trie.Terminal)
        {
            throw new ArgumentException("Subkey already exists", nameof(keys));
        }
        if (throwIfExisting && trie.Contains(ultimateKey))
        {
            throw new ArgumentException("Key already exists", nameof(keys));
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
        IEnumerable<string> keys)
    {
        ITrie<string, object?>? currentTrie = _root;
        var lastTrie = currentTrie;
        string? lastKey = null;

        using var enumerator = keys.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (currentTrie == null)
            {
                lastTrie = null;
                break;
            }

            var key = enumerator.Current;
            lastTrie = currentTrie;
            lastKey = key;

            currentTrie = currentTrie.TryGetChild(key, out var value) ? value : null;
        }

        if (lastTrie == null)
        {
            trie = default;
            ultimateKey = default;
            return false;
        }

        trie = lastTrie;
        ultimateKey = lastKey!;
        return true;
    }

    private ITrie<string, object?> CreateTries(out string ultimateKey, IEnumerable<string> keys)
    {
        ITrie<string, object?> currentTrie = _root;
        string? lastKey = null;

        using var enumerator = keys.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (lastKey == null)
            {
                lastKey = enumerator.Current;
                continue;
            }

            if (!currentTrie.TryGetChild(lastKey, out var trie))
            {
                trie = new Trie<string, object?>(lastKey, null);
                currentTrie[lastKey] = trie;
            }
            else if (trie.Terminal)
            {
                throw new ArgumentException("Subkey already exists.", nameof(keys));
            }
            currentTrie = trie;

            lastKey = enumerator.Current;
        }

        if (lastKey == null)
        {
            throw new ArgumentException("Keys must have value.", nameof(keys));
        }

        ultimateKey = lastKey;
        return currentTrie;
    }

    private static void GetValues<T>(
        IEnumerable<T> initial,
        Func<T, string> keyAction,
        Func<T, object?> valueAction,
        Func<T, IEnumerable<T>> childrenAction,
        Func<T, bool> hasChildrenAction,
        Action<IEnumerable<string>, object?> addAction)
    {
        var nodes = new Stack<(T, int depth)>(initial.Select(i => (i, 1)));
        var currentKeys = new List<string>();

        while (nodes.Count > 0)
        {
            var (trie, depth) = nodes.Pop();

            while (depth < currentKeys.Count)
            {
                currentKeys.RemoveAt(currentKeys.Count - 1);
            }

            if (depth > currentKeys.Count)
            {
                currentKeys.Add(keyAction(trie));
            }
            else if (depth == currentKeys.Count)
            {
                currentKeys[^1] = keyAction(trie);
            }

            if (hasChildrenAction(trie))
            {
                foreach (var child in childrenAction(trie))
                {
                    nodes.Push((child, depth + 1));
                }
            }
            else
            {
                addAction(currentKeys, valueAction(trie));
            }
        }
    }

    private void GetValues(Action<IEnumerable<string>, object?> addAction)
    {
        GetValues(
            _root.Children,
            t => t.Key,
            t => t.Value,
            t => t.Children,
            t => t.Count > 0,
            addAction);
    }

    private class MetadataDebugView
    {
        private readonly MetadataObject _metadata;

        public List<KeyValuePair<string, object?>> Metadata
        {
            get
            {
                return _metadata.GetFlattenedDictionary(".")
                    .OrderBy(p => p.Key)
                    .ToList();
            }
        }

        public MetadataDebugView(MetadataObject metadata)
        {
            _metadata = metadata;
        }
    }
}
