using System;
using System.Collections.Generic;

namespace Wilgysef.Stalk.Core.Shared.MetadataObjects
{
    public interface IMetadataObject
    {
        /// <summary>
        /// Separator character for nested keys.
        /// </summary>
        char KeySeparator { get; set; }

        /// <summary>
        /// Indicates if there are values stored in the object.
        /// </summary>
        bool HasValues { get; }

        /// <summary>
        /// Get/set key value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value</returns>
        object this[string key] { get; set; }

        /// <summary>
        /// Adds value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <exception cref="ArgumentException">Subkey already exists.</exception>
        void Add(string key, object value);

        /// <summary>
        /// Adds value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keyParts">Key parts.</param>
        /// <exception cref="ArgumentException">Subkey already exists.</exception>
        void AddByParts(object value, params string[] keyParts);

        /// <summary>
        /// Sets value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keyParts">Key parts.</param>
        void SetByParts(object value, params string[] keyParts);

        /// <summary>
        /// Adds a new value if it does not exist.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value was added, <see langword="false"/> otherwise.</returns>
        bool TryAddValue(string key, object value);

        /// <summary>
        /// Adds a new value if it does not exist.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keyParts">Key parts.</param>
        /// <returns><see langword="true"/> if the value was added, <see langword="false"/> otherwise.</returns>
        bool TryAddValueByParts(object value, params string[] keyParts);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value.</returns>
        object GetValue(string key);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="keyParts">Key parts.</param>
        /// <returns>Value.</returns>
        object GetValueByParts(params string[] keyParts);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool TryGetValue(string key, out object value);

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="keyParts">Key parts.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool TryGetValueByParts(out object value, params string[] keyParts);

        /// <summary>
        /// Checks if key exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool Contains(string key);

        /// <summary>
        /// Checks if key exists.
        /// </summary>
        /// <param name="keyParts">Key parts.</param>
        /// <returns><see langword="true"/> if the value exists, <see langword="false"/> otherwise.</returns>
        bool ContainsByParts(params string[] keyParts);

        /// <summary>
        /// Removes a key value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><see langword="true"/> if the value was removed, <see langword="false"/> otherwise.</returns>
        bool Remove(string key);

        /// <summary>
        /// Removes a key value.
        /// </summary>
        /// <param name="keyParts">Key parts.</param>
        /// <returns><see langword="true"/> if the value was removed, <see langword="false"/> otherwise.</returns>
        bool RemoveByParts(params string[] keyParts);

        /// <summary>
        /// Copies keys and values.
        /// </summary>
        /// <returns>Copied object.</returns>
        IMetadataObject Copy();

        /// <summary>
        /// Gets an <see cref="IDictionary{TKey, TValue}"/> that contains the nested keys and values.
        /// </summary>
        /// <returns>Keys and values.</returns>
        IDictionary<string, object> GetDictionary();

        /// <summary>
        /// Sets keys and values from the keys and values of an <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="dictionary">Dictionary.</param>
        void From(IDictionary<object, object> dictionary);

        /// <summary>
        /// Gets the key from key parts.
        /// </summary>
        /// <param name="keyParts">Key parts.</param>
        /// <returns>Key.</returns>
        string GetKey(params string[] keyParts);
    }
}
