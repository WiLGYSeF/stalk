﻿using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.FileServices;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.Downloaders;

public sealed class DefaultDownloader : DownloaderBase
{
    public override string Name => "Default";

    public DefaultDownloader(
        IFileService fileService,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        IMetadataSerializer metadataSerializer,
        HttpClient httpClient)
        : base(
            fileService,
            stringFormatter,
            filenameSlugSelector,
            metadataSerializer,
            httpClient)
    { }

    public override bool CanDownload(Uri uri)
    {
        return true;
    }
}
