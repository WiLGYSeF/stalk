using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Wilgysef.Stalk.Core.Shared.Extensions
{
    public static class RegexExtensions
    {
        /// <summary>
        /// Searches the input string for the first occurrence of the regular expression.
        /// </summary>
        /// <param name="regex">Regular expression.</param>
        /// <param name="input">Input.</param>
        /// <param name="match">Regular expression match.</param>
        /// <returns><see langword="true"/> if the regular expression matches, otherwise <see langword="false"/>.</returns>
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

        /// <summary>
        /// Searches the input string for the first occurrence of the regular expression.
        /// </summary>
        /// <param name="regex">Regular expression.</param>
        /// <param name="input">Input.</param>
        /// <param name="startat">Start position.</param>
        /// <param name="match">Regular expression match.</param>
        /// <returns><see langword="true"/> if the regular expression matches, otherwise <see langword="false"/>.</returns>
        public static bool TryMatch(this Regex regex, string input, int startat, [MaybeNullWhen(false)] out Match match)
        {
            match = regex.Match(input, startat);
            if (match.Success)
            {
                return true;
            }

            match = default;
            return false;
        }

        /// <summary>
        /// Searches the input string for the first occurrence of the regular expression.
        /// </summary>
        /// <param name="regex">Regular expression.</param>
        /// <param name="input">Input.</param>
        /// <param name="beginning">Start position.</param>
        /// <param name="length">Number of characters to search.</param>
        /// <param name="match">Regular expression match.</param>
        /// <returns><see langword="true"/> if the regular expression matches, otherwise <see langword="false"/>.</returns>
        public static bool TryMatch(this Regex regex, string input, int beginning, int length, [MaybeNullWhen(false)] out Match match)
        {
            match = regex.Match(input, beginning, length);
            if (match.Success)
            {
                return true;
            }

            match = default;
            return false;
        }
    }
}
