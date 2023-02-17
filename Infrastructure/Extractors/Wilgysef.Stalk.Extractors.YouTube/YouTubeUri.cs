using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeUri
{
    private const string UriPrefixRegex = @"^(?:https?://)?(?:(?:www\.|m\.)?youtube\.com|youtu\.be)";
    private const string ChannelPartRegex = @"(?:(?:channel/(?<channel>[^/]+))|(?:c/(?<c_channel>[^/]+))|(?:@(?<at_channel>[^/]+)))";
    private const string ChannelPrefixRegex = UriPrefixRegex + "/" + ChannelPartRegex;

    private static readonly Regex FeaturedRegex = new(ChannelPrefixRegex + "(?:/(?:featured)?)?$", RegexOptions.Compiled);
    private static readonly Regex VideosRegex = new(ChannelPrefixRegex + "/videos$", RegexOptions.Compiled);
    private static readonly Regex ShortsRegex = new(ChannelPrefixRegex + "/shorts$", RegexOptions.Compiled);
    private static readonly Regex LivestreamsRegex = new(ChannelPrefixRegex + "/streams$", RegexOptions.Compiled);
    private static readonly Regex PlaylistsRegex = new(ChannelPrefixRegex + "/playlists$", RegexOptions.Compiled);
    private static readonly Regex CommunityRegex = new(ChannelPrefixRegex + "/community.*", RegexOptions.Compiled);
    private static readonly Regex MembershipRegex = new(ChannelPrefixRegex + "/membership$", RegexOptions.Compiled);

    private static readonly Regex VideoRegex = new(UriPrefixRegex + @"/watch.*", RegexOptions.Compiled);
    private static readonly Regex ShortRegex = new(UriPrefixRegex + @"/shorts/(?<short>[A-Za-z0-9_-]+)$", RegexOptions.Compiled);
    private static readonly Regex PlaylistRegex = new(UriPrefixRegex + @"/playlist.*", RegexOptions.Compiled);
    private static readonly Regex CommunityPostRegex = new(UriPrefixRegex + @"/post/(?<post>[A-Za-z0-9_-]+)$", RegexOptions.Compiled);

    public YouTubeUriType Type { get; }

    public Uri Uri { get; }

    public string? ChannelNameOrId { get; }

    public ChannelNameType ChannelNameType => _channelNameType;
    private readonly ChannelNameType _channelNameType;

    public bool HasChannelId => ChannelNameType == ChannelNameType.Channel;

    public bool HasChannelName => ChannelNameType == ChannelNameType.AtSymbol;

    public string? ItemId { get; }

    public YouTubeUri(Uri uri)
    {
        Uri = uri;

        var leftUri = uri.GetLeftPart(UriPartial.Path);

        if (FeaturedRegex.TryMatch(leftUri, out var match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Featured;
        }
        else if (VideosRegex.TryMatch(leftUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Videos;
        }
        else if (ShortsRegex.TryMatch(leftUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Shorts;
        }
        else if (LivestreamsRegex.TryMatch(leftUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Livestreams;
        }
        else if (PlaylistsRegex.TryMatch(leftUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Playlists;
        }
        else if (CommunityRegex.TryMatch(Uri.AbsoluteUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);

            var query = Uri.GetQueryParameters()!;
            if (query.TryGetValue("lb", out var postId))
            {
                Type = YouTubeUriType.CommunityPost;
                ItemId = postId;
            }
            else
            {
                Type = YouTubeUriType.Community;
            }
        }
        else if (MembershipRegex.TryMatch(leftUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Membership;
        }
        else if (VideoRegex.TryMatch(Uri.AbsoluteUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Video;

            var query = Uri.GetQueryParameters()!;
            ItemId = query["v"];
        }
        else if (ShortRegex.TryMatch(leftUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Short;
            ItemId = match.Groups["short"].Value;
        }
        else if (PlaylistRegex.TryMatch(Uri.AbsoluteUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.Playlist;

            var query = Uri.GetQueryParameters()!;
            ItemId = query["list"];
        }
        else if (CommunityPostRegex.TryMatch(leftUri, out match))
        {
            ChannelNameOrId = GetChannel(match, out _channelNameType);
            Type = YouTubeUriType.CommunityPost;
            ItemId = match.Groups["post"].Value;
        }
        else
        {
            throw new ArgumentException(null, nameof(uri));
        }
    }

    public string GetChannelUri()
    {
        switch (ChannelNameType)
        {
            case ChannelNameType.Channel:
                return $"https://www.youtube.com/channel/{ChannelNameOrId}/";
            case ChannelNameType.C:
                return $"https://www.youtube.com/c/{ChannelNameOrId}/";
            case ChannelNameType.AtSymbol:
                return $"https://www.youtube.com/@{ChannelNameOrId}/";
            default:
                throw new ArgumentOutOfRangeException(nameof(ChannelNameType));
        }
    }

    public static bool TryGetUri(Uri uri, [MaybeNullWhen(false)] out YouTubeUri youTubeUri)
    {
        try
        {
            youTubeUri = new YouTubeUri(uri);
            return true;
        }
        catch
        {
            youTubeUri = null;
            return false;
        }
    }

    private static string GetChannel(Match match, out ChannelNameType type)
    {
        if (match.Groups["channel"].Success)
        {
            type = ChannelNameType.Channel;
            return match.Groups["channel"].Value;
        }
        else if (match.Groups["c_channel"].Success)
        {
            type = ChannelNameType.C;
            return match.Groups["c_channel"].Value;
        }
        else if (match.Groups["at_channel"].Success)
        {
            type = ChannelNameType.AtSymbol;
            return match.Groups["at_channel"].Value;
        }

        type = ChannelNameType.None;
        return null;
    }
}

public enum YouTubeUriType
{
    Featured,
    Videos,
    Shorts,
    Livestreams,
    Playlists,
    Community,
    Membership,
    Video,
    Short,
    Livestream,
    Playlist,
    CommunityPost,
}

public enum ChannelNameType
{
    None,
    Channel,
    C,
    AtSymbol,
}
