using System.Diagnostics.CodeAnalysis;

namespace Wilgysef.Stalk.Core.Tries;

internal interface ITrie<TKey, TValue> where TKey : notnull
{
    TKey Key { get; set; }

    TValue Value { get; set; }

    bool Terminal { get; set; }

    int Count { get; }

    ITrie<TKey, TValue>? Parent { get; set; }

    ICollection<ITrie<TKey, TValue>> Children { get; }

    ITrie<TKey, TValue> this[TKey key] { get; set; }

    bool TryGetChild(TKey key, [MaybeNullWhen(false)] out ITrie<TKey, TValue> trie);

    bool Contains(TKey key);

    bool Remove(TKey key);
}
