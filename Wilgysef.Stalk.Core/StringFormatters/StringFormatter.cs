using System.Text;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.StringFormatters;

public class StringFormatter : IStringFormatter, ITransientDependency
{
    private static readonly Regex FormatRegex = new(@"(?<=(?:^|[^$])(?:\$\$)*)(\${(?<format>(?:\\}|[^}])+)})", RegexOptions.Compiled);

    private static readonly Regex FormatInternalRegex = new(@"((?<char>^|[,|:])(?<value>(?:""(?<literal>(?:\\""|[^""])*)""|\\,|\\\||\\:|[^,|:])+))", RegexOptions.Compiled);

    private const string FormatRegexFormatGroup = "format";

    private const string FormatInternalRegexCharGroup = "char";
    private const string FormatInternalRegexValueGroup = "value";
    private const string FormatInternalRegexLiteralGroup = "literal";

    private const string FormatInitString = "$";
    private const string FormatInitDoubleString = "$$";

    private const char FormatAlignmentChar = ',';
    private const char FormatFormatterChar = ':';
    private const char FormatDefaultChar = '|';

    public string Format(string value, IDictionary<string, object?> formatValues)
    {
        var builder = new StringBuilder();
        MatchCollection matches = FormatRegex.Matches(value);

        var lastIndex = 0;
        foreach (Match match in matches.Cast<Match>())
        {
            builder.Append(value[lastIndex..match.Index].Replace(FormatInitDoubleString, FormatInitString));
            lastIndex = match.Index + match.Length;

            builder.Append(FormatInternal(match, formatValues));
        }
        builder.Append(value[lastIndex..].Replace(FormatInitDoubleString, FormatInitString));

        return builder.ToString();
    }

    private string FormatInternal(Match match, IDictionary<string, object?> formatValues)
    {
        MatchCollection matches = FormatInternalRegex.Matches(match.Groups[FormatRegexFormatGroup].Value);
        string? key = null;
        string? formatter = null;
        string? alignment = null;
        object? defaultValue = null;

        foreach (Match m in matches.Cast<Match>())
        {
            var charValue = m.Groups[FormatInternalRegexCharGroup].Value;
            if (charValue.Length == 0)
            {
                key ??= m.Groups[FormatInternalRegexValueGroup].Value;
                continue;
            }

            switch (charValue[0])
            {
                case FormatAlignmentChar:
                    alignment = m.Groups[FormatInternalRegexValueGroup].Value;
                    break;
                case FormatFormatterChar:
                    formatter = m.Groups[FormatInternalRegexValueGroup].Value;
                    break;
                case FormatDefaultChar:
                    if (defaultValue == null)
                    {
                        if (m.Groups[FormatInternalRegexLiteralGroup].Success)
                        {
                            defaultValue = m.Groups[FormatInternalRegexLiteralGroup].Value
                                .Replace("\\\"", "\"");
                        }
                        else
                        {
                            formatValues.TryGetValue(m.Groups[FormatInternalRegexValueGroup].Value, out defaultValue);
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        if (!formatValues.TryGetValue(key ?? "", out var value))
        {
            value = defaultValue ?? "";
        }

        string valueStr = FormatObject(value, formatter);

        if (alignment != null && int.TryParse(alignment, out var alignmentValue))
        {
            valueStr = AlignString(valueStr, alignmentValue);
        }

        return valueStr;
    }

    private string FormatObject(object? value, string? format)
    {
        if (format == null)
        {
            return value?.ToString() ?? "";
        }

        // TODO: formatters

        return value?.ToString() ?? "";
    }

    private string AlignString(string value, int alignment)
    {
        var alignmentAbs = Math.Abs(alignment);
        return alignment > 0 ? value.PadLeft(alignmentAbs) : value.PadRight(alignmentAbs);
    }
}
