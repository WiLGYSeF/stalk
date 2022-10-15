using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Wilgysef.Stalk.Core.Shared.Extensions
{
    public static class RegexExtensions
    {
        public static bool TryMatch(this Regex regex, string input, [MaybeNullWhen(false)] out Match match)
        {
            match = regex.Match(input);
            if (match.Success)
            {
                return true;
            }

            match = default;
            return false;
        }
    }
}
