using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Wilgysef.Stalk.Core.Shared.Extensions
{
    public static class IDictionaryExtensions
    {
        public static bool TryGetValueAs<T, TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            [MaybeNullWhen(false)] out T value)
        {
            if (dictionary.TryGetValue(key, out var v) && v is T val)
            {
                value = val;
                return true;
            }

            value = default;
            return false;
        }
    }
}
