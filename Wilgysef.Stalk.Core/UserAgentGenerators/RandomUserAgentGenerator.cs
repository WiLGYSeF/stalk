using System.Security.Cryptography;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.UserAgentGenerators;

public class RandomUserAgentGenerator : IUserAgentGenerator, ITransientDependency
{
    private static readonly object[][] SystemInformations = new[]
    {
        new[] { "Windows NT 10.0; Win64; x64" },
        new[] { "Windows NT 10.0; WOW64" },
        new[] { "Windows NT 6.1; Win64; x64" },
        new[] { "Windows NT 6.3; Win64; x64" },
        new[] { "X11; Linux x86_64" },
        new object[]
        {
            "Macintosh; Intel Mac OS X ",
            new RandomRange(10, 14),
            "-",
            new RandomRange(1, 14),
            "-",
            new RandomRange(1, 9),
        }
    };

    private static readonly object[][] Platforms = new[]
    {
        new object[]
        {
            "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/",
            new RandomRange(50, 100),
            ".",
            new RandomRange(0, 9),
            ".",
            new RandomRange(1000, 5000),
            ".",
            new RandomRange(100, 500),
            " Safari/537.36",
        },
        new object[]
        {
            "Gecko/20100101 Firefox/",
            new RandomRange(50, 100),
            ".0",
        },
    };

    public string Create()
    {
        var systemInformation = string.Join("", SystemInformations[RandomNumberGenerator.GetInt32(0, SystemInformations.Length)]);
        var platform = string.Join("", Platforms[RandomNumberGenerator.GetInt32(0, Platforms.Length)]);
        return $"Mozilla/5.0 ({systemInformation}) {platform}";
    }

    private struct RandomRange
    {
        public Range Range { get; }

        public RandomRange(Range range)
        {
            Range = range;
        }

        public RandomRange(int start, int end)
        {
            Range = new Range(start, end);
        }

        public override string ToString()
        {
            return RandomNumberGenerator.GetInt32(Range.Start.Value, Range.End.Value + 1).ToString();
        }
    }
}
