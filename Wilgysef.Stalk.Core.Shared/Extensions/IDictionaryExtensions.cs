using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Wilgysef.Stalk.Core.Shared.Extensions
{
    public static class IDictionaryExtensions
    {
        /// <summary>
        /// Gets value from dictionary as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <typeparam name="TKey">Dictionary key type.</typeparam>
        /// <typeparam name="TValue">Dictionary value type.</typeparam>
        /// <param name="dictionary">Dictionary.</param>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value exists and is of type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
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
