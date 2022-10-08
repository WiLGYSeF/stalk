using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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

    /// <summary>
    /// <c>-R</c>, <c>--retries</c>
    /// </summary>
    public int Retries { get; set; } = 10;

    /// <summary>
    /// <c>--file-access-retries</c>
    /// </summary>
    public int FileAccessRetries { get; set; } = 3;

    /// <summary>
    /// <c>--fragment-retries</c>
    /// </summary>
    public int FragmentRetries { get; set; } = 10;

    /// <summary>
    /// <c>--retry-sleep</c>
    /// </summary>
    public List<string> RetrySleep { get; set; } = new();

    /// <summary>
    /// <c>--buffer-size</c>
    /// </summary>
    public int BufferSize
    {
        get => _bufferSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(BufferSize), "Buffer size must be greater than zero.");
            }
            _bufferSize = value;
        }
    }
    private int _bufferSize = 1024;

    /// <summary>
    /// <c>--write-info-json</c>
    /// </summary>
    public bool WriteInfoJson { get; set; } = true;

    /// <summary>
    /// <c>--write-subs</c>
    /// </summary>
    public bool WriteSubs { get; set; } = true;

    public bool MoveInfoJsonToMetadata { get; set; } = true;

    private const string ConfigExeName = "executableName";

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

    private readonly ConcurrentDictionary<object, DownloadStatus> _downloadStatuses = new();

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
        // --cookies FILE

        var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
        var filename = filenameSlug.SlugifyPath(
            _stringFormatter.Format(filenameTemplate, metadata.GetFlattenedDictionary()));

        if (!Config.TryGetValue(ConfigExeName, out var exeName))
        {
            exeName = YouTubeDlDefaultExeNames;
        }

        var youtubeDlFilename = Path.Combine(_externalBinariesOptions.Path, exeName.ToString());

        var processStartInfo = new ProcessStartInfo(youtubeDlFilename)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };

        processStartInfo.ArgumentList.Add("--retries");
        processStartInfo.ArgumentList.Add(Retries.ToString());

        processStartInfo.ArgumentList.Add("--file-access-retries");
        processStartInfo.ArgumentList.Add(FileAccessRetries.ToString());

        processStartInfo.ArgumentList.Add("--fragment-retries");
        processStartInfo.ArgumentList.Add(FragmentRetries.ToString());

        foreach (var retry in RetrySleep)
        {
            processStartInfo.ArgumentList.Add("--retry-sleep");
            processStartInfo.ArgumentList.Add(retry);
        }

        processStartInfo.ArgumentList.Add("--buffer-size");
        processStartInfo.ArgumentList.Add(BufferSize.ToString());

        processStartInfo.ArgumentList.Add("--progress");
        processStartInfo.ArgumentList.Add("--newline");

        if (WriteInfoJson)
        {
            processStartInfo.ArgumentList.Add("--write-info-json");
        }
        if (WriteSubs)
        {
            processStartInfo.ArgumentList.Add("--write-subs");
        }

        processStartInfo.ArgumentList.Add("--output");
        processStartInfo.ArgumentList.Add(filename);

        processStartInfo.ArgumentList.Add(uri.AbsoluteUri);

        var process = FindAndStartProcess(processStartInfo);

        var status = new DownloadStatus();
        _downloadStatuses[process] = status;

        process.OutputDataReceived += OutputReceivedHandler;
        process.ErrorDataReceived += ErrorReceivedHandler;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch
        {
            Debug.WriteLine($"Cancelled {process.ExitCode} {process.HasExited}");
            throw;
        }

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
            if (MoveInfoJsonToMetadata)
            {
                try
                {
                    using var stream = _fileSystem.File.Open(status.MetadataFilename, FileMode.Open);
                    using var reader = new StreamReader(stream);

                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();
                    var youtubeMetadata = deserializer.Deserialize(reader);
                    metadata["youtube"] = youtubeMetadata;

                    _fileSystem.File.Delete(status.MetadataFilename);
                }
                catch (Exception exception)
                {
                    Logger?.LogError(exception, "Could not get YouTube metadata.");
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
        if (!Config.TryGetValue(ConfigExeName, out var exeName))
        {
            foreach (var possibleName in YouTubeDlDefaultExeNames)
            {
                try
                {
                    return StartProcess(possibleName);
                }
                catch (Win32Exception) { }
            }

            throw new InvalidOperationException("Could not start youtube-dl.");
        }

        return StartProcess(exeName!.ToString()!);

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
        Debug.WriteLine(args.Data);

        if (sender is not IProcess process
            || args.Data == null
            || !_downloadStatuses.TryGetValue(sender, out var downloadStatus))
        {
            return;
        }

        downloadStatus.Percentage = null;
        downloadStatus.TotalSize = null;
        downloadStatus.AverageBytesPerSecond = null;
        downloadStatus.EstimatedCompletionTime = null;

        if (args.Data.StartsWith(OutputSubtitlesPrefix))
        {
            downloadStatus.SubtitlesFilename = GetFullPath(process, args.Data[OutputSubtitlesPrefix.Length..]);
            downloadStatus.DestinationFilename = downloadStatus.SubtitlesFilename;
        }
        else if (args.Data.StartsWith(OutputMetadataPrefix))
        {
            downloadStatus.MetadataFilename = GetFullPath(process, args.Data[OutputMetadataPrefix.Length..]);
            downloadStatus.DestinationFilename = downloadStatus.MetadataFilename;
        }
        else if (args.Data.StartsWith(OutputOutputPrefix))
        {
            downloadStatus.OutputFilename = GetFullPath(process, args.Data[OutputOutputPrefix.Length..^1]);
            downloadStatus.DestinationFilename = downloadStatus.OutputFilename;
        }
        else if (args.Data.StartsWith(OutputDownloadDestination))
        {
            downloadStatus.DestinationFilename = GetFullPath(process, args.Data[OutputDownloadDestination.Length..]);
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
        Logger?.LogError("YouTube: error: {Error}", args.Data);
    }

    private string GetFullPath(IProcess process, string path)
    {
        return Path.GetFullPath(Path.Combine(process.StartInfo.WorkingDirectory, path));
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
