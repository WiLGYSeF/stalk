namespace Wilgysef.Stalk.Core.StringFormatters.FormatTokens;

internal class FormatToken
{
    public FormatTokenType Type { get; }

    public string Value { get; }

    public FormatToken(FormatTokenType type, string value)
    {
        Type = type;
        Value = value;
    }
}
