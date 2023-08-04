using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Extractors.Pixiv;

public class PixivUri
{
    private static readonly Regex ArtworkRegex = new(@"^(?:https?://)?(?:www\.)?pixiv\.net/(?<language>[A-Za-z]+)/artworks/(?<id>[0-9]+)(?:/|$)", RegexOptions.Compiled);

    public PixivUriType Type { get; set; }

    public string? ArtworkId { get; set; }

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

    public static bool TryGetUri(Uri uri, [MaybeNullWhen(false)] out PixivUri pixivUri)
    {
        var leftUri = uri.GetLeftPart(UriPartial.Path);

        if (ArtworkRegex.TryMatch(leftUri, out var artworkMatch))
        {
            pixivUri = Artwork(artworkMatch.Groups["id"].Value, uri);
            return true;
        }

        pixivUri = null;
        return false;
    }
}

public enum PixivUriType
{
    Artwork,
}
