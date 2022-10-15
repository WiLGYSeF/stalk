using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Wilgysef.Stalk.Core.Shared.Extensions
{
    public static class NameValueCollectionExtensions
    {
        /// <summary>
        /// Gets value from name value collection.
        /// </summary>
        /// <param name="collection">Name value collection.</param>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <returns><see langword="true"/> if the value exists, otherwise <see langword="false"/>.</returns>
        public static bool TryGetValue(this NameValueCollection collection, string key, [MaybeNullWhen(false)] out string value)
        {
            if (collection.AllKeys.Contains(key))
            {
                value = collection[key];
                return true;
            }

            value = default;
            return false;
        }
    }
}
