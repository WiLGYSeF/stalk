using Wilgysef.Stalk.Core.StringFormatters.FormatTokens;

namespace Wilgysef.Stalk.Core.StringFormatters;

internal class FormatTokenizer
{
    private const char FormatBeginChar = '{';
    private const char FormatEndChar = '}';

    private const char FormatAlignmentChar = ',';
    private const char FormatFormatterChar = ':';
    private const char FormatDefaultChar = '|';

    private readonly string FormatBeginString = FormatBeginChar.ToString();
    private readonly string FormatEndString = FormatEndChar.ToString();

    private readonly string FormatAlignmentString = FormatAlignmentChar.ToString();
    private readonly string FormatFormatterString = FormatFormatterChar.ToString();
    private readonly string FormatDefaultString = FormatDefaultChar.ToString();

    public IEnumerable<FormatToken> GetTokens(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == FormatBeginChar)
            {
                yield return new FormatToken(FormatTokenType.FormatBegin, FormatBeginString);
                continue;
            }
            if (value[index] == FormatEndChar)
            {
                yield return new FormatToken(FormatTokenType.FormatEnd, FormatEndString);
                continue;
            }

            var endIndex = index;
            for (; endIndex < value.Length && value[endIndex] != FormatBeginChar && value[endIndex] != FormatEndChar; endIndex++) ;

            if (endIndex != index)
            {
                yield return new FormatToken(FormatTokenType.Constant, value[index..endIndex]);
                index = endIndex - 1;
                continue;
            }
        }
    }

    public List<FormatToken> GetFormatTokens(string value)
    {
        var tokens = new List<FormatToken>();

        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == FormatAlignmentChar)
            {
                tokens.Add(new FormatToken(FormatTokenType.FormatAlignment, FormatAlignmentString));
                continue;
            }
            if (value[index] == FormatFormatterChar)
            {
                tokens.Add(new FormatToken(FormatTokenType.FormatFormatter, FormatFormatterString));
                continue;
            }
            if (value[index] == FormatDefaultChar)
            {
                tokens.Add(new FormatToken(FormatTokenType.FormatDefault, FormatDefaultString));
                continue;
            }

            var endIndex = index;
            for (; endIndex < value.Length && value[endIndex] != FormatAlignmentChar && value[endIndex] != FormatFormatterChar && value[endIndex] != FormatDefaultChar; endIndex++) ;

            if (endIndex != index)
            {
                tokens.Add(new FormatToken(FormatTokenType.Constant, value[index..endIndex]));
                index = endIndex - 1;
                continue;
            }
        }

        return tokens;
    }
}
