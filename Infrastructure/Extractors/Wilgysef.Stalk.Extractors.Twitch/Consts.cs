using System.Text.RegularExpressions;

namespace Wilgysef.Stalk.Extractors.Twitch;

public class Consts
{
    public const string UriPrefixRegex = @"(?:https?://)?(?:www\.)?twitch\.tv";

    public static readonly Regex VideoRegex = new(UriPrefixRegex + @"/videos/(?<video>[0-9]+)", RegexOptions.Compiled);

    public const string GraphQlUri = "https://gql.twitch.tv/gql";

    public const string ClientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
}
