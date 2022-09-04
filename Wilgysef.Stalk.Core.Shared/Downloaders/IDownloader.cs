﻿using System;
using System.Collections.Generic;
using System.Threading;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public interface IDownloader
    {
        bool CanDownload(Uri uri);

        IAsyncEnumerable<DownloadResult> DownloadAsync(
            Uri uri,
            string filenameTemplate,
            string itemId,
            string itemData,
            string metadataTemplate,
            IMetadataObject metadata,
            CancellationToken cancellationToken = default);
    }
}