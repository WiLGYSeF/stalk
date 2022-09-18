using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Extractors
{
    public interface IExtractor
    {
        string Name { get; }

        ILogger? Logger { get; set; }

        ICacheObject<string, object?> Cache { get; set; }

        bool CanExtract(Uri uri);

        IAsyncEnumerable<ExtractResult> ExtractAsync(
            Uri uri,
            string? itemData,
            IMetadataObject? metadata,
            CancellationToken cancellationToken = default);

        void SetHttpClient(HttpClient client);
    }
}
