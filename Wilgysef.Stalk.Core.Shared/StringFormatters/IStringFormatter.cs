using System.Collections.Generic;

namespace Wilgysef.Stalk.Core.Shared.StringFormatters
{
    public interface IStringFormatter
    {
        /// <summary>
        /// Formats string.
        /// </summary>
        /// <param name="value">String to format.</param>
        /// <param name="formatValues">Format values.</param>
        /// <returns>Formatted string.</returns>
        string Format(string value, IDictionary<string, object?> formatValues);
    }
}
