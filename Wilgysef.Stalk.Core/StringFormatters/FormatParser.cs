//using System.Reflection;
//using System.Text;
//using Wilgysef.Stalk.Core.StringFormatters.FormatTokens;

//namespace Wilgysef.Stalk.Core.StringFormatters;

//internal class FormatParser
//{
//    private readonly StringBuilder _builder = new();

//    private IList<FormatToken> _tokens;

//    private int _index = 0;

//    private FormatToken? CurrentToken => _index < _tokens.Count ? _tokens[_index] : null;

//    private readonly IDictionary<string, object> _formatValues;

//    public FormatParser(IDictionary<string, object> formatValues)
//    {
//        _formatValues = formatValues;
//    }

//    public string Parse(IList<FormatToken> tokens)
//    {
//        _tokens = tokens;

//        while (_index < _tokens.Count)
//        {
//            if(!FormatPart())
//            {
//                for (var i = _index; i < _tokens.Count; i++)
//                {
//                    _builder.Append(_tokens[i].GetTokenValue());
//                }
//                break;
//            }
//        }

//        return _builder.ToString();
//    }

//    private bool FormatPart()
//    {
//        if (ConstantWithoutInit())
//        {
//            return true;
//        }
//        if (FormatTemplate())
//        {
//            return true;
//        }
//        return false;
//    }

//    private bool FormatTemplate()
//    {
//        var tokenBuffer = new List<FormatToken>();
//        var success = false;

//        try
//        {
//            ThrowIf(CurrentToken!.Type != FormatTokenType.FormatInit);
//            tokenBuffer.Add(CurrentToken);
//            NextToken();

//            // TODO: handle escape

//            ThrowIf(CurrentToken!.Type != FormatTokenType.FormatBegin);
//            tokenBuffer.Add(CurrentToken);
//            NextToken();

//            var formatInternal = FormatInternal(tokenBuffer);
//            ThrowIf(formatInternal == null);

//            if (CurrentToken.Type != FormatTokenType.FormatEnd)
//            {

//            }

//            success = true;
//        }
//        catch (Exception)
//        {
//            foreach (var token in tokenBuffer)
//            {
//                _builder.Append(token.GetTokenValue());
//            }
//        }

//        return success;
//    }

//    private FormatInternal? FormatInternal(IList<FormatToken> tokenBuffer)
//    {
//        if (CurrentToken?.Type != FormatTokenType.Constant)
//        {
//            return null;
//        }

//        var formatInternal = new FormatInternal
//        {
//            Key = CurrentToken.GetTokenValue(),
//        };
//        tokenBuffer.Add(CurrentToken);
//        NextToken();

//        do
//        {
//            switch (CurrentToken.Type)
//            {
//                case FormatTokenType.FormatFormatter:
//                    NextToken();
//                    if (CurrentToken.Type == FormatTokenType.FormatEnd)
//                case FormatTokenType.FormatAlignment:
//                case FormatTokenType.FormatDefault:
//                default:
//                    return null;
//            }
//        }
//    }

//    private bool ConstantWithoutInit()
//    {
//        if (ConstantWithoutInitQuote())
//        {
//            return true;
//        }
//        if (CurrentToken?.Type != FormatTokenType.FormatQuote)
//        {
//            return false;
//        }

//        AppendToken();
//        ConstantWithoutInit();
//        return true;
//    }

//    private bool ConstantWithoutInitQuote()
//    {
//        switch (CurrentToken?.Type)
//        {
//            case FormatTokenType.Constant:
//                AppendToken();
//                return true;
//            case FormatTokenType.FormatBegin:
//            case FormatTokenType.FormatEnd:
//            case FormatTokenType.FormatAlignment:
//            case FormatTokenType.FormatFormatter:
//            case FormatTokenType.FormatDefault:
//                AppendToken();
//                ConstantWithoutInitQuote();
//                return true;
//            default:
//                return false;
//        }
//    }

//    private void AppendToken()
//    {
//        _builder.Append(CurrentToken!.GetTokenValue());
//        NextToken();
//    }

//    private void NextToken()
//    {
//        _index++;
//    }

//    private void ThrowIf(bool condition)
//    {
//        ThrowIf(condition, new Exception());
//    }

//    private void ThrowIf(bool condition, Exception exception)
//    {
//        if (condition)
//        {
//            throw exception;
//        }
//    }

//    private class FormatInternal
//    {
//        public string Key { get; set; } = null!;

//        public string? Formatter { get; set; }

//        public string? Alignment { get; set; }

//        public List<string> Defaults { get; set; } = new();
//    }

//    // format = { format-part }
//    // format-part = constant-without-init | format-template
//    // format-template = format-init, format-begin, format-internal, format-end
//    // format-internal =
//    //     constant,
//    //     {
//    //         format-formatter, constant
//    //         | format-alignment, constant
//    //         | format-default, default-value
//    //     }
//    // default-value = constant | format-quote, quoted-constant, format-quote
//    // constant-without-init =
//    //     constant-without-init-quote
//    //     | format-quote [ constant-without-init ]
//    // constant-without-init-quote =
//    //     constant
//    //     | format-begin [ constant-without-init-quote ]
//    //     | format-end [ constant-without-init-quote ]
//    //     | format-alignment [ constant-without-init-quote ]
//    //     | format-formatter [ constant-without-init-quote ]
//    //     | format-default [ constant-without-init-quote ]
//    // quoted-constant =
//    //     constant-without-init-quote
//    //     | format-init [ quoted-constant ]
//    //     | format-quote, format-quote [ quoted-constant ]
//}

// (?<=(?:^|[^$])(?:\$\$)*)(\${((?:\\}|[^}])+)})
