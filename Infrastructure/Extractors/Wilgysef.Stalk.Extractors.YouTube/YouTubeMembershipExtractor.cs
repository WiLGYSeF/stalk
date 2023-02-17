using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Wilgysef.Stalk.Core.Shared.DateTimeProviders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.YouTube;

internal class YouTubeMembershipExtractor : YouTubeExtractorBase
{
    private readonly YouTubeCommunityExtractor _communityExtractor;

    public YouTubeMembershipExtractor(
        HttpClient httpClient,
        YouTubeCommunityExtractor communityExtractor,
        IDateTimeProvider dateTimeProvider,
        YouTubeExtractorConfig config)
        : base(httpClient, dateTimeProvider, config)
    {
        _communityExtractor = communityExtractor;
    }

    public async IAsyncEnumerable<ExtractResult> ExtractMembershipAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var json = await GetMembershipJsonAsync(uri, cancellationToken);
        var channelId = json.SelectTokens("$..channelId").FirstOrDefault()?.ToString()
            ?? json.SelectToken("$..authorEndpoint.browseEndpoint.browseId")!.ToString();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contents = json.SelectToken("$..itemSectionRenderer.contents")
                ?? json.SelectToken("$..continuationItems");
            if (contents == null)
            {
                break;
            }

            foreach (var content in contents)
            {
                var video = content.SelectToken("$.videoRenderer");
                var post = content.SelectToken("$.backstagePostThreadRenderer");

                if (video != null)
                {
                    foreach (var result in ExtractVideo(video, metadata))
                    {
                        yield return result;
                    }
                }
                else if (post != null)
                {
                    var postRenderer = post.SelectToken("$..backstagePostRenderer");
                    foreach (var result in _communityExtractor.ExtractCommunity(postRenderer!, metadata))
                    {
                        yield return result;
                    }
                }
            }

            var continuationToken = json.SelectToken("$..continuationCommand.token")?.ToString();
            if (string.IsNullOrEmpty(continuationToken))
            {
                break;
            }

            json = await GetMembershipJsonAsync(channelId, continuationToken, cancellationToken);
        }
    }

    private IEnumerable<ExtractResult> ExtractVideo(JToken video, IMetadataObject metadata)
    {
        var videoId = video.SelectToken("$.videoId")!.ToString();

        yield return new ExtractResult(
            $"https://www.youtube.com/watch?v={videoId}",
            videoId,
            JobTaskType.Extract,
            metadata: metadata);
    }

    private async Task<JObject> GetMembershipJsonAsync(Uri uri, CancellationToken cancellationToken)
    {
        return await GetYtInitialData(uri, cancellationToken);
    }

    private async Task<JObject> GetMembershipJsonAsync(
        string channelId,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        return await GetBrowseJsonAsync(
            $"https://www.youtube.com/channel/{channelId}/membership",
            continuationToken,
            cancellationToken);
    }
}
