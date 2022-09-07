using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Extractors
{
    public interface IExtractor
    {
        string Name { get; }

        ILogger Logger { get; set; }

        bool CanExtract(Uri uri);

        IAsyncEnumerable<ExtractResult> ExtractAsync(
            Uri uri,
            string itemData,
            IMetadataObject metadata,
            CancellationToken cancellationToken = default);
    }
}
