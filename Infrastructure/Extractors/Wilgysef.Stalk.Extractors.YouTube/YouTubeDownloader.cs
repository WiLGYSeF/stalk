﻿using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.Options;
using Wilgysef.Stalk.Core.Shared.ProcessServices;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using Wilgysef.Stalk.Extractors.YoutubeDl.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeDownloader : DownloaderBase
{
    public override string Name => "YouTube";

    private static readonly Regex OutputOutputRegex = new($@"\[Merger\] Merging formats into \""(?<{YoutubeDlRunner.OutputOutputRegexGroup}>.*)\""$", RegexOptions.Compiled);

    private YouTubeDownloaderConfig _config = new();

    private readonly IFileSystem _fileSystem;
    private readonly IStringFormatter _stringFormatter;
    private readonly IFilenameSlugSelector _filenameSlugSelector;
    private readonly ExternalBinariesOptions _externalBinariesOptions;
    private readonly IProcessService _processService;

    public YouTubeDownloader(
        IFileSystem fileSystem,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        IMetadataSerializer metadataSerializer,
        ExternalBinariesOptions externalBinariesOptions,
        IProcessService processService,
        HttpClient httpClient)
        : base(
            fileSystem,
            stringFormatter,
            filenameSlugSelector,
            metadataSerializer,
            httpClient)
    {
        _fileSystem = fileSystem;
        _stringFormatter = stringFormatter;
        _filenameSlugSelector = filenameSlugSelector;
        _externalBinariesOptions = externalBinariesOptions;
        _processService = processService;
    }

    public override bool CanDownload(Uri uri)
    {
        return Consts.VideoRegex.IsMatch(uri.AbsoluteUri);
    }

    protected override async IAsyncEnumerable<DownloadFileResult> SaveFileAsync(
        Uri uri,
        string filenameTemplate,
        IMetadataObject metadata,
        DownloadRequestData? requestData = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _config = new(Config);

        var youtubeDl = new YoutubeDlRunner(_processService, OutputOutputRegex)
        {
            Config = _config.ToYoutubeDlConfig(),
            Logger = Logger,
        };

        var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
        var filename = filenameSlug.SlugifyPath(
            _stringFormatter.Format(filenameTemplate, metadata.GetFlattenedDictionary()));

        CreateDirectoriesFromFilename(filename);

        var status = new DownloadStatus();
        var process = youtubeDl.FindAndStartProcess(uri, filename, status);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Logger?.LogError(exception, "YouTube: Failed to run youtube-dl: {ExitCode}", process.ExitCode);
            throw;
        }

        if (status.OutputFilename != null)
        {
            status.OutputFilename = GetFullPath(process, status.OutputFilename);
        }
        if (status.MetadataFilename != null)
        {
            status.MetadataFilename = GetFullPath(process, status.MetadataFilename);
        }
        if (status.SubtitlesFilename != null)
        {
            status.SubtitlesFilename = GetFullPath(process, status.SubtitlesFilename);
        }
        status.DestinationFilename = null;

        if (status.SubtitlesFilename != null)
        {
            yield return new DownloadFileResult(
                status.SubtitlesFilename,
                null,
                null,
                null,
                null);
        }

        if (status.MetadataFilename != null)
        {
            if (_config.MoveInfoJsonToMetadata)
            {
                try
                {
                    using (var stream = _fileSystem.File.Open(status.MetadataFilename, FileMode.Open))
                    {
                        using var reader = new StreamReader(stream);

                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();
                        var youtubeMetadata = deserializer.Deserialize(reader);
                        metadata["youtube_dl"] = youtubeMetadata;
                    }

                    _fileSystem.File.Delete(status.MetadataFilename);
                }
                catch (Exception exception)
                {
                    Logger?.LogError(exception, "YouTube: Could not get YouTube metadata.");
                }
            }
            else
            {
                yield return new DownloadFileResult(
                    status.MetadataFilename,
                    null,
                    null,
                    null,
                    null);
            }
        }

        yield return new DownloadFileResult(
            status.OutputFilename,
            null,
            null,
            null,
            null,
            createMetadata: true);
    }

    private string GetFullPath(IProcess process, string path)
    {
        return Path.GetFullPath(Path.Combine(process.StartInfo.WorkingDirectory, path));
    }

    private void CreateDirectoriesFromFilename(string filename)
    {
        var dirname = Path.GetDirectoryName(filename);
        if (!string.IsNullOrEmpty(dirname))
        {
            _fileSystem.Directory.CreateDirectory(dirname);
        }
    }
}
