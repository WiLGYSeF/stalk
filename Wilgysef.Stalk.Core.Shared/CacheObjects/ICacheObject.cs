using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Wilgysef.Stalk.Core.Shared.CacheObjects
{
    public interface ICacheObject
    {
        /// <summary>
        /// Cache keys.
        /// </summary>
        ICollection<object> Keys { get; }

        /// <summary>
        /// Gets or sets cache values.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <returns>Cache value.</returns>
        object? this[object key] { get; set; }

        /// <summary>
        /// Adds value to cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="expires">Cache expiration.</param>
        void Add(object key, object? value, DateTime? expires = null);

        /// <summary>
        /// Adds value to cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="expires">Cache expiration.</param>
        /// <returns><see langword="true"/> if the value was added, otherwise <see langword="false"/>.</returns>
        bool TryAdd(object key, object? value, DateTime? expires = null);

        /// <summary>
        /// Sets value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="expires">Cache expiration.</param>
        void Set(object key, object? value, DateTime? expires = null);

        /// <summary>
        /// Gets cached value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value exists, otherwise <see langword="false"/>.</returns>
        bool TryGetValue(object key, out object? value);

        /// <summary>
        /// Gets cached value.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value exists, otherwise <see langword="false"/>.</returns>
        bool TryGetValueAs<T>(object key, out T value);

        /// <summary>
        /// Checks if the key is in the cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><see langword="true"/> if the value exists, otherwise <see langword="false"/>.</returns>
        bool ContainsKey(object key);

        /// <summary>
        /// Removes key from cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><see langword="true"/> if the value was removed, otherwise <see langword="false"/>.</returns>
        bool Remove(object key);

        /// <summary>
        /// Removes key from cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value was removed, otherwise <see langword="false"/>.</returns>
        bool Remove(object key, out object? value);

        /// <summary>
        /// Clears cache.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removes expired values.
        /// </summary>
        /// <returns>Number of values removed.</returns>
        int RemoveExpired();
    }

    public interface ICacheObject<TKey, TValue>
    {
        /// <summary>
        /// Cache keys.
        /// </summary>
        ICollection<TKey> Keys { get; }

        /// <summary>
        /// Gets or sets cache values.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <returns>Cache value.</returns>
        TValue this[TKey key] { get; set; }

        /// <summary>
        /// Adds value to cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="expires">Cache expiration.</param>
        void Add(TKey key, TValue value, DateTime? expires = null);

        /// <summary>
        /// Adds value to cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="expires">Cache expiration.</param>
        /// <returns><see langword="true"/> if the value was added, otherwise <see langword="false"/>.</returns>
        bool TryAdd(TKey key, TValue value, DateTime? expires = null);

        /// <summary>
        /// Sets value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="expires">Cache expiration.</param>
        void Set(TKey key, TValue value, DateTime? expires = null);

        /// <summary>
        /// Gets cached value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value exists, otherwise <see langword="false"/>.</returns>
        bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

        /// <summary>
        /// Gets cached value.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value exists, otherwise <see langword="false"/>.</returns>
        bool TryGetValueAs<T>(TKey key, out T value);

        /// <summary>
        /// Checks if the key is in the cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><see langword="true"/> if the value exists, otherwise <see langword="false"/>.</returns>
        bool ContainsKey(TKey key);

        /// <summary>
        /// Removes key from cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><see langword="true"/> if the value was removed, otherwise <see langword="false"/>.</returns>
        bool Remove(TKey key);

        /// <summary>
        /// Removes key from cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value was removed, otherwise <see langword="false"/>.</returns>
        bool Remove(TKey key, out TValue value);

        /// <summary>
        /// Clears cache.
        /// </summary>
        void Clear();

        /// <summary>
        /// Cleans up and removes expired values.
        /// </summary>
        /// <returns>Number of values removed.</returns>
        int RemoveExpired();
    }
}
