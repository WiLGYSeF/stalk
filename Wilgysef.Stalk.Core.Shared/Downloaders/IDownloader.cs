using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public interface IDownloader
    {
        /// <summary>
        /// Downloader name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Logger.
        /// </summary>
        ILogger? Logger { get; set; }

        /// <summary>
        /// Downloader config.
        /// </summary>
        IDictionary<string, object?> Config { get; set; }

        /// <summary>
        /// Indicates if the downloader is able to download from the URI.
        /// </summary>
        /// <param name="uri">URI to download from.</param>
        /// <returns><see langword="true"/> if the downloader can download from the URI, otherwise <see langword="false"/>.</returns>
        bool CanDownload(Uri uri);

        /// <summary>
        /// Downloads from the URI.
        /// </summary>
        /// <param name="uri">URI to download from.</param>
        /// <param name="filenameTemplate">Filename template to save downloaded files.</param>
        /// <param name="itemId">Item Id.</param>
        /// <param name="metadataTemplate">Metadata filename template to save metadata files.</param>
        /// <param name="metadata">Metadata object.</param>
        /// <param name="requestData">Download request data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Download results.</returns>
        IAsyncEnumerable<DownloadResult> DownloadAsync(
            Uri uri,
            string filenameTemplate,
            string? itemId,
            string? metadataTemplate,
            IMetadataObject metadata,
            DownloadRequestData? requestData = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the HTTP client that should be used.
        /// </summary>
        /// <param name="client">HTTP client.</param>
        void SetHttpClient(HttpClient client);
    }
}
