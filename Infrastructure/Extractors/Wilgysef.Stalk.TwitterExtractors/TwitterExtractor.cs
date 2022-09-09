using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.TwitterExtractors;

public class TwitterExtractor : IExtractor
{
    public string Name => "Twitter";

    public ILogger? Logger { get; set; }

    private Regex _uriRegex = new(@"(?:https?://)?(?:www\.)?twitter\.com(?:\:(?:80|443))?/(?<user>[^/]+)(?:/status/(?<tweet>[0-9]+))?", RegexOptions.Compiled);

    public bool CanExtract(Uri uri)
    {
        return _uriRegex.IsMatch(uri.AbsoluteUri);
    }

    public IAsyncEnumerable<ExtractResult> ExtractAsync(Uri uri, string itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void SetHttpClient(HttpClient client)
    {
        throw new NotImplementedException();
    }
}
