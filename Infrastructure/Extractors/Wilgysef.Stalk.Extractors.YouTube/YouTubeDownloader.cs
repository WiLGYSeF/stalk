﻿using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
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
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeDownloader : DownloaderBase
{
    public override string Name => "YouTube";

    private static readonly string[] YouTubeDlDefaultExeNames = new string[]
    {
        "youtube-dl.exe",
        "youtube-dl",
        "yt-dlp.exe",
        "yt-dlp"
    };

    private static readonly string OutputMetadataPrefix = "[info] Writing video metadata as JSON to: ";
    private static readonly string OutputSubtitlesPrefix = "[info] Writing video subtitles to: ";
    private static readonly string OutputOutputPrefix = "[Merger] Merging formats into \"";
    private static readonly string OutputDownloadDestination = "[download] Destination: ";
    private static readonly Regex OutputDownloadProgressShort = new(@"^\[download\]\s+(?<size>[0-9.]+(?:[KMG]i?)?B) at \s+(?<rate>Unknown B/s|[0-9.]+(?:[KMG]i?)?B/s)\s+\([0-9:]+\)", RegexOptions.Compiled);
    private static readonly Regex OutputDownloadProgress = new(@"^\[download\]\s+(?<percent>[0-9.]+)% of (?<size>[0-9.]+(?:[KMG]i?)?B) at \s+(?<rate>Unknown B/s|[0-9.]+(?:[KMG]i?)?B/s) ETA\s+(?<eta>Unknown|[0-9:]+)", RegexOptions.Compiled);
    private static readonly Regex SizeRegex = new(@"^(?<amount>[0-9.]+)(?<size>(?:[KMG]i?)?B)(?:/s)?$", RegexOptions.Compiled);

    private readonly ConcurrentDictionary<int, DownloadStatus> _downloadStatuses = new();

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

        var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
        var filename = filenameSlug.SlugifyPath(
            _stringFormatter.Format(filenameTemplate, metadata.GetFlattenedDictionary()));

        CreateDirectoriesFromFilename(filename);

        var processStartInfo = new ProcessStartInfo()
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };

        processStartInfo.ArgumentList.Add("--retries");
        processStartInfo.ArgumentList.Add(_config.Retries.ToString());

        processStartInfo.ArgumentList.Add("--file-access-retries");
        processStartInfo.ArgumentList.Add(_config.FileAccessRetries.ToString());

        processStartInfo.ArgumentList.Add("--fragment-retries");
        processStartInfo.ArgumentList.Add(_config.FragmentRetries.ToString());

        foreach (var retry in _config.RetrySleep)
        {
            processStartInfo.ArgumentList.Add("--retry-sleep");
            processStartInfo.ArgumentList.Add(retry);
        }

        processStartInfo.ArgumentList.Add("--buffer-size");
        processStartInfo.ArgumentList.Add(_config.BufferSize.ToString());

        processStartInfo.ArgumentList.Add("--progress");
        processStartInfo.ArgumentList.Add("--newline");

        if (_config.WriteInfoJson)
        {
            processStartInfo.ArgumentList.Add("--write-info-json");
        }
        if (_config.WriteSubs)
        {
            processStartInfo.ArgumentList.Add("--write-subs");
        }

        if (_config.CookieString != null)
        {
            processStartInfo.ArgumentList.Add("--add-header");
            processStartInfo.ArgumentList.Add($"Cookie:{_config.CookieString}");
        }

        processStartInfo.ArgumentList.Add("--output");
        processStartInfo.ArgumentList.Add(filename);

        processStartInfo.ArgumentList.Add(uri.AbsoluteUri);

        var process = FindAndStartProcess(processStartInfo);

        var status = new DownloadStatus();
        _downloadStatuses[process.Id] = status;

        process.OutputDataReceived += OutputReceivedHandler;
        process.ErrorDataReceived += ErrorReceivedHandler;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

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
                        metadata["youtube"] = youtubeMetadata;
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

    private IProcess FindAndStartProcess(ProcessStartInfo startInfo)
    {
        var combineExternalBinaryPath = _externalBinariesOptions.Path != null && _externalBinariesOptions.Path.Length > 0;
        if (_config.ExecutableName != null)
        {
            return StartProcess(_config.ExecutableName.ToString());
        }

        foreach (var possibleName in YouTubeDlDefaultExeNames)
        {
            try
            {
                return StartProcess(possibleName);
            }
            catch (Win32Exception) { }
        }

        throw new InvalidOperationException("Could not start youtube-dl.");

        IProcess StartProcess(string executableName)
        {
            startInfo.FileName = combineExternalBinaryPath
                ? Path.Combine(_externalBinariesOptions.Path!, executableName)
                : executableName;
            return _processService.Start(startInfo)
                ?? throw new InvalidOperationException($"Could not start process: {startInfo.FileName}");
        }
    }

    private void OutputReceivedHandler(object sender, DataReceivedEventArgs args)
    {
        if (args.Data == null)
        {
            return;
        }

        Logger?.LogDebug("YouTube: {Output}", args.Data);

        DownloadStatus downloadStatus;
        try
        {
            // sender could either be a Process or IProcess :(
            dynamic senderProcess = sender;
            int processId = senderProcess.Id;
            if (!_downloadStatuses.TryGetValue(processId, out downloadStatus!))
            {
                Logger?.LogError("YouTube: Could not get download status object from process Id {ProcessId}.", processId);
                return;
            }
        }
        catch (Exception exception)
        {
            Logger?.LogError(exception, "YouTube: Could not get process Id.");
            return;
        }

        downloadStatus.Percentage = null;
        downloadStatus.TotalSize = null;
        downloadStatus.AverageBytesPerSecond = null;
        downloadStatus.EstimatedCompletionTime = null;

        if (args.Data.StartsWith(OutputSubtitlesPrefix))
        {
            downloadStatus.SubtitlesFilename = args.Data[OutputSubtitlesPrefix.Length..];
            downloadStatus.DestinationFilename = downloadStatus.SubtitlesFilename;
        }
        else if (args.Data.StartsWith(OutputMetadataPrefix))
        {
            downloadStatus.MetadataFilename = args.Data[OutputMetadataPrefix.Length..];
            downloadStatus.DestinationFilename = downloadStatus.MetadataFilename;
        }
        else if (args.Data.StartsWith(OutputOutputPrefix))
        {
            downloadStatus.OutputFilename = args.Data[OutputOutputPrefix.Length..^1];
            downloadStatus.DestinationFilename = downloadStatus.OutputFilename;
        }
        else if (args.Data.StartsWith(OutputDownloadDestination))
        {
            downloadStatus.DestinationFilename = args.Data[OutputDownloadDestination.Length..];
        }
        else
        {
            var match = OutputDownloadProgressShort.Match(args.Data);
            if (match.Success)
            {
                downloadStatus.AverageBytesPerSecond = SizeToBytes(match.Groups["rate"].Value);
            }
            else
            {
                match = OutputDownloadProgress.Match(args.Data);
                if (match.Success)
                {
                    downloadStatus.Percentage = double.Parse(match.Groups["percent"].Value) / 100;
                    downloadStatus.TotalSize = SizeToBytes(match.Groups["size"].Value);
                    downloadStatus.AverageBytesPerSecond = SizeToBytes(match.Groups["rate"].Value);

                    var eta = match.Groups["eta"].Value;
                    downloadStatus.EstimatedCompletionTime = eta != "Unknown"
                        ? ParseTimeSpan(eta)
                        : null;
                }
            }
        }
    }

    private void ErrorReceivedHandler(object sender, DataReceivedEventArgs args)
    {
        if (args.Data != null)
        {
            Logger?.LogError("YouTube: error: {Error}", args.Data);
        }
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

    private static long? SizeToBytes(string size)
    {
        if (size.StartsWith("Unknown"))
        {
            return null;
        }

        var match = SizeRegex.Match(size);
        return (long)(double.Parse(match.Groups["amount"].Value)
            * match.Groups["size"].Value[0] switch
            {
                'B' => 1,
                'K' => 1024,
                'M' => 1024 * 1024,
                'G' => 1024 * 1024 * 1024,
            });
    }

    private static TimeSpan ParseTimeSpan(string timeSpan)
    {
        return timeSpan.Count(c => c == ':') == 1
            ? TimeSpan.ParseExact(timeSpan, "mm':'ss", CultureInfo.InvariantCulture)
            : TimeSpan.Parse(timeSpan);
    }

    private class DownloadStatus
    {
        public string? OutputFilename { get; set; }

        public string? MetadataFilename { get; set; }

        public string? SubtitlesFilename { get; set; }

        public string? DestinationFilename { get; set; }

        public double? Percentage { get; set; }

        public long? TotalSize { get; set; }

        public long? AverageBytesPerSecond { get; set; }

        public TimeSpan? EstimatedCompletionTime { get; set; }
    }
}