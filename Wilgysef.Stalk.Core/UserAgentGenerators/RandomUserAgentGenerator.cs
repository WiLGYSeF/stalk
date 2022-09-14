using System.Security.Cryptography;
using System.Text;

namespace Wilgysef.Stalk.Core.UserAgentGenerators;

public class RandomUserAgentGenerator : IUserAgentGenerator
{
    private static readonly RandomRangeString[] SystemInformations = new[]
    {
        new RandomRangeString("Windows NT 10.0; Win64; x64"),
        new RandomRangeString("Windows NT 10.0; WOW64"),
        new RandomRangeString("Windows NT 6.1; Win64; x64"),
        new RandomRangeString("Windows NT 6.3; Win64; x64"),
        new RandomRangeString("X11; Linux x86_64"),
        new RandomRangeString(
            new StringRange("Macintosh; Intel Mac OS X "),
            new StringRange(10, 14),
            new StringRange("_"),
            new StringRange(1, 14),
            new StringRange("_"),
            new StringRange(1, 9)),
    };

    private static readonly RandomRangeString[] Platforms = new[]
    {
        new RandomRangeString(
            new StringRange("AppleWebKit/537.36 (KHTML, like Gecko) Chrome/"),
            new StringRange(50, 100),
            new StringRange("."),
            new StringRange(0, 9),
            new StringRange("."),
            new StringRange(1000, 5000),
            new StringRange("."),
            new StringRange(100, 500),
            new StringRange(" Safari/537.36")),
        new RandomRangeString(
            new StringRange("Gecko/20100101 Firefox/"),
            new StringRange(50, 100),
            new StringRange(".0")),
    };

    public string Generate()
    {
        return $"Mozilla/5.0 ({SystemInformations[RandomNumberGenerator.GetInt32(0, SystemInformations.Length)]}) {Platforms[RandomNumberGenerator.GetInt32(0, Platforms.Length)]}";
    }

    private class RandomRangeString
    {
        private readonly StringBuilder _builder = new();

        private readonly StringRange[] _stringRanges;

        public RandomRangeString(string @string)
        {
            _stringRanges = new[] { new StringRange(@string) };
        }

        public RandomRangeString(params StringRange[] stringRanges)
        {
            _stringRanges = stringRanges;
        }

        public override string ToString()
        {
            _builder.Clear();
            foreach (var stringRange in _stringRanges)
            {
                _builder.Append(stringRange.ToString());
            }
            return _builder.ToString();
        }
    }

    private class StringRange
    {
        public string? String { get; }

        public Range? Range { get; }

        public StringRange(string @string)
        {
            String = @string;
        }

        public StringRange(Range range)
        {
            Range = range;
        }

        public StringRange(int start, int end)
        {
            Range = new Range(start, end);
        }

        public StringRange(string? @string, Range? range)
        {
            String = @string;
            Range = range;

            CheckValues();
        }

        public StringRange(string? @string, int? start, int? end)
        {
            String = @string;
            Range = start.HasValue && end.HasValue ? new Range(start.Value, end.Value) : null;

            CheckValues();
        }

        public override string ToString()
        {
            return String
                ?? RandomNumberGenerator.GetInt32(Range!.Value.Start.Value, Range.Value.End.Value + 1)
                    .ToString();
        }

        private void CheckValues()
        {
            if (String == null && Range == null)
            {
                throw new ArgumentNullException(nameof(String), "String cannot be null if Range is null.");
            }
            if (String != null && Range != null)
            {
                throw new ArgumentException(nameof(Range), "String and Range cannot both be not null.");
            }
        }
    }
}
