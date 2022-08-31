using Wilgysef.Stalk.Core.StringFormatters.FormatTokens;

namespace Wilgysef.Stalk.Core.StringFormatters;

internal class FormatTokenizer
{
    public static string FormatInitString = FormatInitChar.ToString();
    public static string FormatBeginString = FormatBeginChar.ToString();
    public static string FormatEndString = FormatEndChar.ToString();
    public static string FormatAlignmentString = FormatAlignmentChar.ToString();
    public static string FormatFormatterString = FormatFormatterChar.ToString();
    public static string FormatDefaultString = FormatDefaultChar.ToString();

    private const char FormatInitChar = '$';
    private const char FormatBeginChar = '{';
    private const char FormatEndChar = '}';

    private const char FormatAlignmentChar = ',';
    private const char FormatFormatterChar = ':';
    private const char FormatDefaultChar = '|';

    public IEnumerable<FormatToken> GetTokens(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            switch (value[index])
            {
                case FormatInitChar:
                    yield return new FormatToken(FormatTokenType.FormatInit);
                    break;
                case FormatBeginChar:
                    yield return new FormatToken(FormatTokenType.FormatBegin);
                    break;
                case FormatEndChar:
                    yield return new FormatToken(FormatTokenType.FormatEnd);
                    break;
                case FormatAlignmentChar:
                    yield return new FormatToken(FormatTokenType.FormatAlignment);
                    break;
                case FormatFormatterChar:
                    yield return new FormatToken(FormatTokenType.FormatFormatter);
                    break;
                case FormatDefaultChar:
                    yield return new FormatToken(FormatTokenType.FormatDefault);
                    break;
            }

            var endIndex = index;
            while (endIndex < value.Length && IsConstantChar(value[endIndex]))
            {
                endIndex++;
            }

            if (endIndex != index)
            {
                yield return new FormatToken(FormatTokenType.Constant, value[index..endIndex]);
                index = endIndex - 1;
            }
        }
    }

    private bool IsConstantChar(char c)
    {
        return c switch
        {
            FormatInitChar => false,
            FormatBeginChar => false,
            FormatEndChar => false,
            FormatAlignmentChar => false,
            FormatFormatterChar => false,
            FormatDefaultChar => false,
            _ => true,
        };
    }
}
