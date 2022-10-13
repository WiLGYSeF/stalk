using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Wilgysef.Stalk.Core.Shared.Extensions
{
    public static class NameValueCollectionExtensions
    {
        public static bool TryGetValue(this NameValueCollection collection, string key, [MaybeNullWhen(false)] out string value)
        {
            if (!collection.AllKeys.Contains(key))
            {
                value = default;
                return false;
            }

            value = collection[key];
            return true;
        }
    }
}
