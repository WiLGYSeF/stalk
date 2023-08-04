using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.Pixiv;

public class PixivExtractor : IExtractor
{
    private static readonly string[] MetadataArtworkIdKeys = new string[] { "artwork", "id" };
    private static readonly string[] MetadataArtworkTitleKeys = new string[] { "artwork", "title" };
    private static readonly string[] MetadataArtworkDescriptionKeys = new string[] { "artwork", "description" };
    private static readonly string[] MetadataArtworkIllustTypeKeys = new string[] { "artwork", "illustType" };
    private static readonly string[] MetadataArtworkCreateDateKeys = new string[] { "artwork", "createDate" };
    private static readonly string[] MetadataArtworkUploadDateKeys = new string[] { "artwork", "uploadDate" };
    private static readonly string[] MetadataArtworkRestrictKeys = new string[] { "artwork", "restrict" };
    private static readonly string[] MetadataArtworkXRestrictKeys = new string[] { "artwork", "xRestrict" };
    private static readonly string[] MetadataArtworkTagsKeys = new string[] { "artwork", "tags" };
    private static readonly string[] MetadataArtworkUserIdKeys = new string[] { "artwork", "user", "id" };
    private static readonly string[] MetadataArtworkUserNameKeys = new string[] { "artwork", "user", "name" };
    private static readonly string[] MetadataArtworkUserAccountKeys = new string[] { "artwork", "user", "account" };
    private static readonly string[] MetadataArtworkWidthKeys = new string[] { "artwork", "width" };
    private static readonly string[] MetadataArtworkHeightKeys = new string[] { "artwork", "height" };
    private static readonly string[] MetadataArtworkBookmarkCountKeys = new string[] { "artwork", "bookmarkCount" };
    private static readonly string[] MetadataArtworkLikeCountKeys = new string[] { "artwork", "likeCount" };
    private static readonly string[] MetadataArtworkCommentCountKeys = new string[] { "artwork", "commentCount" };
    private static readonly string[] MetadataArtworkResponseCountKeys = new string[] { "artwork", "responseCount" };
    private static readonly string[] MetadataArtworkViewCountKeys = new string[] { "artwork", "viewCount" };
    private static readonly string[] MetadataArtworkIsHowtoKeys = new string[] { "artwork", "isHowto" };
    private static readonly string[] MetadataArtworkIsOriginalKeys = new string[] { "artwork", "isOriginal" };
    private static readonly string[] MetadataArtworkIsUnlistedKeys = new string[] { "artwork", "isUnlisted" };
    private static readonly string[] MetadataArtworkAiTypeKeys = new string[] { "artwork", "aiType" };

    public string Name => "Pixiv";

    public string Version => "2023.08.03";

    public ILogger? Logger { get; set; }
    public ICacheObject<string, object?>? Cache { get; set; }
    public IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

    private HttpClient _httpClient;

    public PixivExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanExtract(Uri uri)
    {
        return PixivUri.TryGetUri(uri, out _);
    }

    public IAsyncEnumerable<ExtractResult> ExtractAsync(Uri uri, string? itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        if (!PixivUri.TryGetUri(uri, out var pixivUri))
        {
            throw new ArgumentException("Invalid URI", nameof(uri));
        }

        return pixivUri.Type switch
        {
            PixivUriType.Artwork => ExtractArtworkAsync(pixivUri.ArtworkId, itemData, metadata, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(uri)),
        };
    }

    public string? GetItemId(Uri uri)
    {
        if (!PixivUri.TryGetUri(uri, out var pixivUri))
        {
            return null;
        }

        return pixivUri.Type switch
        {
            PixivUriType.Artwork => pixivUri.ArtworkId,
            _ => null,
        };
    }

    public void SetHttpClient(HttpClient client)
    {
        _httpClient = client;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractArtworkAsync(
        string artworkId,
        string? itemData,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var pageResponse = await _httpClient.GetAsync($"https://www.pixiv.net/en/artworks/{artworkId}", cancellationToken);
        pageResponse.EnsureSuccessStatusCode();

        var doc = new HtmlDocument();
        doc.Load(await pageResponse.Content.ReadAsStreamAsync(cancellationToken));
        var preloadData = doc.DocumentNode.SelectSingleNode(@"//meta[@id=""meta-preload-data""]");
        var preloadedDataJson = JObject.Parse(preloadData.Attributes["content"].Value);

        var illust = preloadedDataJson.SelectToken($"illust.{artworkId}")!;

        metadata = metadata.Copy();
        metadata[MetadataArtworkIdKeys] = illust["id"]?.ToString();
        metadata[MetadataArtworkTitleKeys] = illust["title"]?.ToString();
        metadata[MetadataArtworkDescriptionKeys] = illust["description"]?.ToString();
        metadata[MetadataArtworkIllustTypeKeys] = illust["illustType"]?.Value<int>();
        metadata[MetadataArtworkCreateDateKeys] = illust["createDate"]?.ToString();
        metadata[MetadataArtworkUploadDateKeys] = illust["uploadDate"]?.ToString();
        metadata[MetadataArtworkRestrictKeys] = illust["restrict"]?.Value<int>();
        metadata[MetadataArtworkXRestrictKeys] = illust["xRestrict"]?.Value<int>();
        metadata[MetadataArtworkTagsKeys] = GetTags(illust["tags"]?["tags"]);
        metadata[MetadataArtworkUserIdKeys] = illust["userId"]?.ToString();
        metadata[MetadataArtworkUserNameKeys] = illust["userName"]?.ToString();
        metadata[MetadataArtworkUserAccountKeys] = illust["userAccount"]?.ToString();
        metadata[MetadataArtworkBookmarkCountKeys] = illust["bookmarkCount"]?.Value<int>();
        metadata[MetadataArtworkLikeCountKeys] = illust["likeCount"]?.Value<int>();
        metadata[MetadataArtworkCommentCountKeys] = illust["commentCount"]?.Value<int>();
        metadata[MetadataArtworkResponseCountKeys] = illust["responseCount"]?.Value<int>();
        metadata[MetadataArtworkViewCountKeys] = illust["viewCount"]?.Value<int>();
        metadata[MetadataArtworkIsHowtoKeys] = illust["isHowto"]?.Value<bool>();
        metadata[MetadataArtworkIsOriginalKeys] = illust["isOriginal"]?.Value<bool>();
        metadata[MetadataArtworkIsUnlistedKeys] = illust["isUnlisted"]?.Value<bool>();
        metadata[MetadataArtworkAiTypeKeys] = illust["aiType"]?.Value<int>();

        var meta = metadata.Copy();

        meta[MetadataArtworkWidthKeys] = illust["width"]?.Value<int>();
        meta[MetadataArtworkHeightKeys] = illust["height"]?.Value<int>();

        var pageCount = illust["pageCount"]?.Value<int>();
        var hasMultiple = !pageCount.HasValue || pageCount.Value > 1;

        yield return GetExtractResult(
            illust["urls"]!["original"]!.ToString(),
            hasMultiple ? $"{artworkId}#1" : artworkId,
            meta);

        if (hasMultiple)
        {
            var imagesResponse = await _httpClient.GetAsync($"https://www.pixiv.net/ajax/illust/{artworkId}/pages?lang=en&version=19921c54619a740796d32683244aad17e288a534", cancellationToken);
            imagesResponse.EnsureSuccessStatusCode();

            var json = JObject.Parse(await imagesResponse.Content.ReadAsStringAsync(cancellationToken));
            var urls = json["body"]!.ToList();

            // skip the first result since it was already extracted
            for (var i = 1; i < urls.Count; i++)
            {
                var url = urls[i];
                meta = metadata.Copy();

                meta[MetadataArtworkWidthKeys] = url["width"]?.Value<int>();
                meta[MetadataArtworkHeightKeys] = url["height"]?.Value<int>();

                yield return GetExtractResult(
                    url["urls"]!["original"]!.ToString(),
                    $"{artworkId}#{i + 1}",
                    meta);
            }
        }

        static ExtractResult GetExtractResult(string url, string itemId, IMetadataObject metadata)
        {
            return new ExtractResult(
                url,
                itemId,
                JobTaskType.Download,
                metadata: metadata,
                downloadRequestData: new DownloadRequestData(
                    headers: new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Referer", "https://www.pixiv.net")
                    }));
        }

        static List<Dictionary<string, object?>> GetTags(JToken? tags)
        {
            var results = new List<Dictionary<string, object?>>();

            if (tags == null)
            {
                return results;
            }

            foreach (var tag in tags)
            {
                if (tag == null)
                {
                    continue;
                }

                var tagObj = new Dictionary<string, object?>
                {
                    ["tag"] = tag["tag"]?.ToString()
                };

                if (tag["romaji"] is JToken romaji)
                {
                    tagObj["romaji"] = romaji.ToString();
                }

                if (tag["translation"] is JToken translation)
                {
                    var translations = new Dictionary<string, string>();

                    foreach (var trans in translation)
                    {
                        if (trans is JProperty prop)
                        {
                            translations[prop.Name] = prop.Value.ToString();
                        }
                    }

                    tagObj["translation"] = translations;
                }

                results.Add(tagObj);
            }

            return results;
        }
    }
}
