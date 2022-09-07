using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.TwitterExtractors;

public class TwitterExtractor : IExtractor
{
    public string Name => "Twitter";

    public ILogger? Logger { get; set; }

    public bool CanExtract(Uri uri)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ExtractResult> ExtractAsync(Uri uri, string itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}