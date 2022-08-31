namespace Wilgysef.Stalk.Core.StringFormatters.FormatTokens;

internal class FormatToken
{
    public FormatTokenType Type { get; }

    public string? Value { get; }

    public FormatToken(FormatTokenType type, string? value = null)
    {
        Type = type;
        Value = value;
    }

    public string GetTokenValue()
    {
        return Type switch
        {
            FormatTokenType.Constant => Value ?? throw new NullReferenceException(),
            FormatTokenType.FormatInit => FormatTokenizer.FormatInitString,
            FormatTokenType.FormatBegin => FormatTokenizer.FormatBeginString,
            FormatTokenType.FormatEnd => FormatTokenizer.FormatEndString,
            FormatTokenType.FormatAlignment => FormatTokenizer.FormatAlignmentString,
            FormatTokenType.FormatFormatter => FormatTokenizer.FormatFormatterString,
            FormatTokenType.FormatDefault => FormatTokenizer.FormatDefaultString,
            _ => throw new ArgumentOutOfRangeException(nameof(Type)),
        };
    }
}
