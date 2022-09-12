using System.Diagnostics.CodeAnalysis;

namespace Wilgysef.Stalk.Core.Tries;

internal class Trie<TKey, TValue> : ITrie<TKey, TValue> where TKey : notnull
{
    public TKey Key { get; set; }

    public TValue Value { get; set; }

    public bool Terminal { get; set; }

    public int Count => _children.Count;

    public ITrie<TKey, TValue>? Parent { get; set; }

    public ICollection<ITrie<TKey, TValue>> Children => new List<ITrie<TKey, TValue>>(_children.Values);

    private readonly Dictionary<TKey, ITrie<TKey, TValue>> _children = new();

    public ITrie<TKey, TValue> this[TKey key]
    {
        get => _children[key];
        set
        {
            _children[key] = value;
            value.Parent = this;
        }
    }

    public Trie(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    public bool TryGetChild(TKey key, [MaybeNullWhen(false)] out ITrie<TKey, TValue> trie)
    {
        return _children.TryGetValue(key, out trie);
    }

    public bool Contains(TKey key)
    {
        return _children.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        if (!TryGetChild(key, out var trie))
        {
            return false;
        }

        trie.Parent = null;
        return _children.Remove(key);
    }

    public void Clear()
    {
        foreach (var child in _children.Values)
        {
            child.Parent = null;
        }
        _children.Clear();
    }
}
