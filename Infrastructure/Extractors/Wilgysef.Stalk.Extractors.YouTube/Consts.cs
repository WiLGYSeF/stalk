using System.Text.RegularExpressions;

namespace Wilgysef.Stalk.Extractors.YouTube;

public static class Consts
{
    public const string UriPrefixRegex = @"^(?:https?://)?(?:(?:www\.|m\.)?youtube\.com|youtu\.be)";
    public const string ChannelPrefixRegex = UriPrefixRegex + @"/(?<segment>c(?:hannel)?)/(?<channel>[A-Za-z0-9_-]+)";

    public static readonly Regex ChannelRegex = new(ChannelPrefixRegex, RegexOptions.Compiled);
    public const string ChannelRegexChannelSegmentGroup = "segment";
    public const string ChannelRegexChannelGroup = "channel";

    public static readonly Regex VideosRegex = new(ChannelPrefixRegex + "/videos", RegexOptions.Compiled);
    public const string VideosRegexChannelSegmentGroup = "segment";
    public const string VideosRegexChannelGroup = "channel";

    public static readonly Regex PlaylistRegex = new(UriPrefixRegex + @"/playlist\?", RegexOptions.Compiled);
    public static readonly Regex VideoRegex = new(UriPrefixRegex + @"/watch\?", RegexOptions.Compiled);
    public static readonly Regex CommunityRegex = new(ChannelPrefixRegex + "/community", RegexOptions.Compiled);
    public const string CommunityRegexChannelSegmentGroup = "segment";
    public const string CommunityRegexChannelGroup = "channel";
}
