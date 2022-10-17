using System;
using System.Collections.Generic;

namespace Wilgysef.Stalk.Core.Shared.MetadataObjects
{
    public interface IMetadataObject
    {
        /// <summary>
        /// Indicates if there are values stored in the object.
        /// </summary>
        bool HasValues { get; }

        /// <summary>
        /// Get/set key value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value</returns>
        object? this[params string[] keys] { get; set; }

        /// <summary>
        /// Adds value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keys">Keys.</param>
        /// <exception cref="ArgumentException">Subkey already exists.</exception>
        void Add(object? value, params string[] keys);

        /// <summary>
        /// Adds value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keys">Keys.</param>
        /// <exception cref="ArgumentException">Subkey already exists.</exception>
        void Add(object? value, IEnumerable<string> keys);

        /// <summary>
        /// Adds a new value if it does not exist.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value was added, <see langword="false"/> otherwise.</returns>
        bool TryAddValue(object? value, params string[] keys);

        /// <summary>
        /// Adds a new value if it does not exist.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value was added, <see langword="false"/> otherwise.</returns>
        bool TryAddValue(object? value, IEnumerable<string> keys);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <returns>Value.</returns>
        object? GetValue(params string[] keys);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <returns>Value.</returns>
        object? GetValue(IEnumerable<string> keys);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="keys">Keys.</param>
        /// <returns>Value.</returns>
        T GetValueAs<T>(params string[] keys);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="keys">Keys.</param>
        /// <returns>Value.</returns>
        T GetValueAs<T>(IEnumerable<string> keys);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool TryGetValue(out object? value, params string[] keys);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool TryGetValue(out object? value, IEnumerable<string> keys);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value.</param>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool TryGetValueAs<T>(out T value, params string[] keys);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value.</param>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool TryGetValueAs<T>(out T value, IEnumerable<string> keys);

        /// <summary>
        /// Checks if key exists.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool Contains(params string[] keys);

        /// <summary>
        /// Checks if key exists.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool Contains(IEnumerable<string> keys);

        /// <summary>
        /// Removes a key value.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value was removed, <see langword="false"/> otherwise.</returns>
        bool Remove(params string[] keys);

        /// <summary>
        /// Removes a key value.
        /// </summary>
        /// <param name="keys">Keys.</param>
        /// <returns><see langword="true"/> if the value was removed, <see langword="false"/> otherwise.</returns>
        bool Remove(IEnumerable<string> keys);

        /// <summary>
        /// Clears metadata.
        /// </summary>
        void Clear();

        /// <summary>
        /// Copies keys and values.
        /// </summary>
        /// <returns>Copied object.</returns>
        IMetadataObject Copy();

        /// <summary>
        /// Gets an <see cref="IDictionary{TKey, TValue}"/> that contains the nested keys and values.
        /// </summary>
        /// <returns>Keys and values.</returns>
        IDictionary<string, object?> GetDictionary();

        /// Gets an <see cref="IDictionary{TKey, TValue}"/> that contains the nested keys and values, flattened to a single layer.
        /// </summary>
        /// <param name="separator">Keys separator.</param>
        /// <returns>Keys and values.</returns>
        IDictionary<string, object?> GetFlattenedDictionary(string separator);

        /// <summary>
        /// Sets keys and values from the keys and values of an <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="dictionary">Dictionary.</param>
        void From(IDictionary<object, object?> dictionary);

        /// <summary>
        /// Sets keys and values from the keys and values of an <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="dictionary">Dictionary.</param>
        void From(IDictionary<string, object?> dictionary);
    }
}
