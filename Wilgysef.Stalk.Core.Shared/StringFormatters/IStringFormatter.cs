using System.Collections.Generic;

namespace Wilgysef.Stalk.Core.Shared.StringFormatters
{
    public interface IStringFormatter
    {
        string Format(string value, IDictionary<string, object?> formatValues);
    }
}
