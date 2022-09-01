using System.Text;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.StringFormatters;

public class StringFormatter : IStringFormatter, ITransientDependency
{
    private static Regex _formatRegex = new(@"(?<=(?:^|[^$])(?:\$\$)*)(\${(?<format>(?:\\}|[^}])+)})", RegexOptions.Compiled);

    private static Regex _formatInternalRegex = new(@"((?<char>^|[,|:])(?<value>(?:""(?<literal>(?:\\""|[^""])*)""|\\,|\\\||\\:|[^,|:])+))", RegexOptions.Compiled);

    private const string _formatRegexFormatGroup = "format";

    private const string _formatInternalRegexCharGroup = "char";
    private const string _formatInternalRegexValueGroup = "value";
    private const string _formatInternalRegexLiteralGroup = "literal";

    private const char _formatInitChar = '$';
    private const char _formatBeginChar = '{';
    private const char _formatEndChar = '}';

    private const char _formatAlignmentChar = ',';
    private const char _formatFormatterChar = ':';
    private const char _formatDefaultChar = '|';

    public string Format(string value, IDictionary<string, object> formatValues)
    {
        var builder = new StringBuilder();
        MatchCollection matches = _formatRegex.Matches(value);

        var lastIndex = 0;
        foreach (Match match in matches.Cast<Match>())
        {
            builder.Append(value[lastIndex..match.Index].Replace("$$", "$"));
            lastIndex = match.Index + match.Length;

            builder.Append(FormatInternal(match, formatValues));
        }
        builder.Append(value[lastIndex..].Replace("$$", "$"));

        return builder.ToString();
    }

    private string FormatInternal(Match match, IDictionary<string, object> formatValues)
    {
        MatchCollection matches = _formatInternalRegex.Matches(match.Groups[_formatRegexFormatGroup].Value);
        string? key = null;
        string? formatter = null;
        string? alignment = null;
        object? defaultValue = null;

        foreach (Match m in matches.Cast<Match>())
        {
            var charValue = m.Groups[_formatInternalRegexCharGroup].Value;
            if (charValue.Length == 0)
            {
                key ??= m.Groups[_formatInternalRegexValueGroup].Value;
                continue;
            }

            switch (charValue[0])
            {
                case _formatAlignmentChar:
                    alignment = m.Groups[_formatInternalRegexValueGroup].Value;
                    break;
                case _formatFormatterChar:
                    formatter = m.Groups[_formatInternalRegexValueGroup].Value;
                    break;
                case _formatDefaultChar:
                    if (defaultValue == null)
                    {
                        if (m.Groups[_formatInternalRegexLiteralGroup].Success)
                        {
                            defaultValue = m.Groups[_formatInternalRegexLiteralGroup].Value
                                .Replace("\\\"", "\"");
                        }
                        else
                        {
                            formatValues.TryGetValue(m.Groups[_formatInternalRegexValueGroup].Value, out defaultValue);
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

    private string FormatObject(object value, string? format)
    {
        if (format == null)
        {
            return value.ToString() ?? "";
        }

        // TODO: formatters

        return value.ToString() ?? "";
    }

    private string AlignString(string value, int alignment)
    {
        var alignmentAbs = Math.Abs(alignment);
        return alignment > 0 ? value.PadLeft(alignmentAbs) : value.PadRight(alignmentAbs);
    }
}
