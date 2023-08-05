using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Extractors.Pixiv;

public class PixivUri
{
    private static readonly Regex ArtworkRegex = new(@"^(?:https?://)?(?:www\.)?pixiv\.net/(?<language>[A-Za-z]+/)?artworks/(?<id>[0-9]+)(?:/|$)", RegexOptions.Compiled);
    private static readonly Regex UserArtworksRegex = new(@"^(?:https?://)?(?:www\.)?pixiv\.net/(?<language>[A-Za-z]+/)?users/(?<id>[0-9]+)/artworks(?:/|$)", RegexOptions.Compiled);
    private static readonly Regex UserRegex = new(@"^(?:https?://)?(?:www\.)?pixiv\.net/(?<language>[A-Za-z]+/)?users/(?<id>[0-9]+)(?:/|$)", RegexOptions.Compiled);

    public PixivUriType Type { get; set; }

    public string? ArtworkId { get; set; }

    public string? UserId { get; set; }

    public Uri Uri { get; }

    private PixivUri(Uri uri, PixivUriType type)
    {
        Uri = uri;
        Type = type;
    }

    public static PixivUri Artwork(string artworkId, Uri uri)
    {
        return new PixivUri(uri, PixivUriType.Artwork)
        {
            ArtworkId = artworkId,
        };
    }

    public static PixivUri User(string userId, Uri uri)
    {
        return new PixivUri(uri, PixivUriType.User)
        {
            UserId = userId,
        };
    }

    public static PixivUri UserArtworks(string userId, Uri uri)
    {
        return new PixivUri(uri, PixivUriType.UserArtworks)
        {
            UserId = userId,
        };
    }

    public static bool TryGetUri(Uri uri, [MaybeNullWhen(false)] out PixivUri pixivUri)
    {
        var leftUri = uri.GetLeftPart(UriPartial.Path);

        if (ArtworkRegex.TryMatch(leftUri, out var artworkMatch))
        {
            pixivUri = Artwork(artworkMatch.Groups["id"].Value, uri);
            return true;
        }
        else if (UserArtworksRegex.TryMatch(leftUri, out var userArtworkMatch))
        {
            pixivUri = User(userArtworkMatch.Groups["id"].Value, uri);
            return true;
        }
        else if (UserRegex.TryMatch(leftUri, out var userMatch))
        {
            pixivUri = User(userMatch.Groups["id"].Value, uri);
            return true;
        }

        pixivUri = null;
        return false;
    }
}

public enum PixivUriType
{
    Artwork,
    User,
    UserArtworks,
}
