﻿using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.ProcessServices;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using Wilgysef.Stalk.Extractors.YoutubeDl.Core;

namespace Wilgysef.Stalk.Extractors.Twitch;

public class TwitchDownloader : DownloaderBase
{
    public override string Name => "Twitch";

    private static readonly Regex OutputOutputRegex = new($@"\[download\] Destination: (?<{YoutubeDlRunner.OutputOutputRegexGroup}>.*\.mp4)$", RegexOptions.Compiled);

    private TwitchDownloaderConfig _config = new();

    private readonly IFileSystem _fileSystem;
    private readonly IProcessService _processService;

    public TwitchDownloader(
        IFileSystem fileSystem,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        IMetadataSerializer metadataSerializer,
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
        _processService = processService;
    }

    public override bool CanDownload(Uri uri)
    {
        return Consts.VideoRegex.IsMatch(uri.GetLeftPart(UriPartial.Path));
    }

    protected override async IAsyncEnumerable<DownloadFileResult> SaveFileAsync(
        Uri uri,
        string filenameTemplate,
        IMetadataObject metadata,
        DownloadRequestData? requestData = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _config = new(Config);

        using var youtubeDl = new YoutubeDlRunner(_processService, _fileSystem, OutputOutputRegex)
        {
            Config = _config.ToYoutubeDlConfig(),
            Logger = Logger,
        };

        var filename = GetFormattedSlugifiedFilename(filenameTemplate, metadata);

        CreateDirectoriesFromFilename(filename);

        var status = new DownloadStatus();
        var process = await youtubeDl.FindAndStartProcessAsync(
            uri.AbsoluteUri,
            filename,
            status,
            cancellationToken: cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (status.OutputFilename == null)
        {
            throw new InvalidOperationException("Could not get output filename.");
        }

        status.OutputFilename = GetFullPath(process, status.OutputFilename);
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
                    await MoveInfoJsonToMetadataAsync(status.MetadataFilename, metadata);
                }
                catch (Exception exception)
                {
                    Logger?.LogError(exception, "Twitch: Could not get Twitch metadata.");
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

    private async Task MoveInfoJsonToMetadataAsync(string metadataFilename, IMetadataObject metadata)
    {
        using (var stream = _fileSystem.File.Open(metadataFilename, FileMode.Open))
        {
            using var reader = new StreamReader(stream);
            var youtubeMetadata = JsonSerializer.Deserialize<IDictionary<string, object>>(await reader.ReadToEndAsync());
            metadata["youtube_dl"] = youtubeMetadata;
        }

        _fileSystem.File.Delete(metadataFilename);
    }

    private string GetFullPath(IProcess process, string path)
    {
        return _fileSystem.Path.GetFullPath(
            _fileSystem.Path.Combine(process.StartInfo.WorkingDirectory, path));
    }
}
