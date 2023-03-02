using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Extractors
{
    public interface IExtractor : IDisposable
    {
        /// <summary>
        /// Extractor name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Extractor version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Logger.
        /// </summary>
        ILogger? Logger { get; set; }

        /// <summary>
        /// Cache.
        /// </summary>
        ICacheObject<string, object?>? Cache { get; set; }

        /// <summary>
        /// Extractor config.
        /// </summary>
        IDictionary<string, object?> Config { get; set; }

        /// <summary>
        /// Indicates if the extractor is able to extract from the URI.
        /// </summary>
        /// <param name="uri">URI to extract from.</param>
        /// <returns><see langword="true"/> if the extractor can extract from the URI, otherwise <see langword="false"/>.</returns>
        bool CanExtract(Uri uri);

        /// <summary>
        /// Gets the item Id from the URI.
        /// </summary>
        /// <param name="uri">URI.</param>
        /// <returns>Item id, or <see langword="null"/> if none could be determined.</returns>
        string? GetItemId(Uri uri);

        IAsyncEnumerable<ExtractResult> ExtractAsync(
            Uri uri,
            string? itemData,
            IMetadataObject metadata,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the HTTP client that should be used.
        /// </summary>
        /// <param name="client">HTTP client.</param>
        void SetHttpClient(HttpClient client);
    }
}
