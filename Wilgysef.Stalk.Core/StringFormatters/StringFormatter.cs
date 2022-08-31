using System.Text;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using Wilgysef.Stalk.Core.StringFormatters.FormatTokens;
using YamlDotNet.Core.Tokens;

namespace Wilgysef.Stalk.Core.StringFormatters;

public class StringFormatter : IStringFormatter, ITransientDependency
{
    private static Regex _formatRegex = new(@"(?<=(?:^|[^$])(?:\$\$)*)(\${(?<format>(?:\\}|[^}])+)})", RegexOptions.Compiled);

    private static Regex _formatInternalRegex = new(@"((?<char>^|[,|:])(?<value>(?:""(?<literal>(?:\\""|[^""])*)""|\\,|\\\||\\:|[^,|:])+))", RegexOptions.Compiled);

    public string Format(string value, IDictionary<string, object> formatValues)
    {
        var builder = new StringBuilder();
        MatchCollection matches = _formatRegex.Matches(value);

        var lastIndex = 0;
        foreach (Match match in matches)
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
        MatchCollection matches = _formatInternalRegex.Matches(match.Groups["format"].Value);
        string? formatter = null;
        string? alignment = null;
        string? defaultValue = null;
        return "";
    }

    //public string Format(string value, IDictionary<string, object> formatValues)
    //{
    //    var tokens = new FormatTokenizer().GetTokens(value);
    //    return new FormatParser().Parse(tokens, formatValues);
    //}

    //public string Format(string value, IDictionary<string, object> formatValues)
    //{
    //    var tokenizer = new FormatTokenizer();
    //    using var tokenEnumerator = tokenizer.GetTokens(value).GetEnumerator();

    //    var builder = new StringBuilder();
    //    FormatToken? lastToken = null;
    //    var inFormat = false;

    //    while (tokenEnumerator.MoveNext())
    //    {
    //        var token = tokenEnumerator.Current;

    //        switch (token.Type)
    //        {
    //            case FormatTokenType.Constant:
    //                if (inFormat)
    //                {
    //                    builder.Append(FormatFormatter(tokenizer, token.Value, formatValues));
    //                }
    //                else
    //                {
    //                    builder.Append(token.Value);
    //                }
    //                break;
    //            case FormatTokenType.FormatBegin:
    //                if (lastToken?.Type == FormatTokenType.FormatBegin)
    //                {
    //                    inFormat = !inFormat;
    //                    if (!inFormat)
    //                    {
    //                        builder.Append('{');
    //                    }
    //                }
    //                else
    //                {
    //                    inFormat = true;
    //                }
    //                break;
    //            case FormatTokenType.FormatEnd:
    //                if (!inFormat)
    //                {
    //                    // TODO: do not write double close braces
    //                    builder.Append('}');
    //                }
    //                inFormat = false;
    //                break;
    //            default:
    //                throw new NotImplementedException();
    //        }

    //        lastToken = token;
    //    }

    //    if (lastToken?.Type == FormatTokenType.FormatBegin)
    //    {
    //        builder.Append('{');
    //    }

    //    return builder.ToString();
    //}

    //private string FormatFormatter(FormatTokenizer tokenizer, string value, IDictionary<string, object> formatValues)
    //{
    //    var tokens = tokenizer.GetFormatTokens(value);
    //    object? result = null;
    //    var alignment = GetFormatTokenValue(tokens, FormatTokenType.FormatAlignment, out _);
    //    var formatter = GetFormatTokenValue(tokens, FormatTokenType.FormatFormatter, out _);
    //    var defaultValue = GetFormatDefaultTokenValue(tokens, formatValues, out _);

    //    if (tokens.Count > 0 && tokens[0].Type == FormatTokenType.Constant)
    //    {
    //        formatValues.TryGetValue(tokens[0].Value, out result);
    //    }

    //    string resultValue;
    //    if (formatter != null)
    //    {
    //        throw new NotImplementedException();
    //    }
    //    else
    //    {
    //        resultValue = (result ?? "").ToString() ?? "";
    //    }

    //    if (result == null && defaultValue != null)
    //    {
    //        resultValue = defaultValue;
    //    }

    //    if (alignment != null)
    //    {
    //        if (int.TryParse(alignment, out var alignmentValue))
    //        {
    //            resultValue = AlignString(resultValue, alignmentValue);
    //        }
    //    }

    //    return resultValue;
    //}

    //private string? GetFormatTokenValue(IList<FormatToken> tokens, FormatTokenType type, out int index)
    //{
    //    string? result = null;
    //    index = -1;

    //    for (var i = 0; i < tokens.Count; i++)
    //    {
    //        if (tokens[i].Type != type)
    //        {
    //            continue;
    //        }

    //        index = i + 1;
    //        if (i < tokens.Count - 1 && tokens[i + 1].Type == FormatTokenType.Constant)
    //        {
    //            result = tokens[i + 1].Value;
    //        }
    //    }

    //    return result;
    //}

    //private string? GetFormatDefaultTokenValue(IList<FormatToken> tokens, IDictionary<string, object> formatValues, out int index)
    //{
    //    var result = GetFormatTokenValue(tokens, FormatTokenType.FormatDefault, out index);
    //    if (index == -1 || result == null)
    //    {
    //        return null;
    //    }

    //    if (result.StartsWith('"'))
    //    {
    //        result = result[1..];
    //        return result[^1] == '"' ? result[..^1] : result;
    //    }
    //    if (!formatValues.TryGetValue(result, out var resultValue))
    //    {
    //        return null;
    //    }
    //    return resultValue.ToString();
    //}

    //private string AlignString(string value, int alignment)
    //{
    //    var alignmentAbs = Math.Abs(alignment);
    //    return alignment > 0 ? value.PadLeft(alignmentAbs) : value.PadRight(alignmentAbs);
    //}
}
