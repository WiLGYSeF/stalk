using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Wilgysef.Stalk.Core.Shared.Extensions;
using Wilgysef.Stalk.Core.Shared.ProcessServices;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public class YoutubeDlRunner : IDisposable
    {
        /// <summary>
        /// Regex group name of the output file.
        /// </summary>
        public static string OutputOutputRegexGroup = "output";

        private static readonly string OutputMetadataPrefix = "[info] Writing video metadata as JSON to: ";
        private static readonly string OutputSubtitlesPrefix = "[info] Writing video subtitles to: ";
        private static readonly string OutputDownloadDestination = "[download] Destination: ";
        private static readonly Regex OutputDownloadProgressShort = new Regex(@"^\[download\]\s+(?<size>[0-9.]+(?:[KMG]i?)?B) at \s+(?<rate>Unknown B/s|[0-9.]+(?:[KMG]i?)?B/s)\s+\([0-9:]+\)", RegexOptions.Compiled);
        private static readonly Regex OutputDownloadProgress = new Regex(@"^\[download\]\s+(?<percent>[0-9.]+)% of (?<size>[0-9.]+(?:[KMG]i?)?B) at \s+(?<rate>Unknown B/s|[0-9.]+(?:[KMG]i?)?B/s) ETA\s+(?<eta>Unknown|[0-9:]+)", RegexOptions.Compiled);
        private static readonly Regex SizeRegex = new Regex(@"^(?<amount>[0-9.]+)(?<size>(?:[KMG]i?)?B)(?:/s)?$", RegexOptions.Compiled);

        private readonly ConcurrentDictionary<int, IDownloadStatus> _downloadStatuses = new ConcurrentDictionary<int, IDownloadStatus>();
        private readonly ConcurrentDictionary<int, Func<string, IDownloadStatus, bool>> _outputCallbacks = new ConcurrentDictionary<int, Func<string, IDownloadStatus, bool>>();
        private readonly ConcurrentDictionary<int, Func<string, IDownloadStatus, bool>> _errorCallbacks = new ConcurrentDictionary<int, Func<string, IDownloadStatus, bool>>();

        private readonly ConcurrentDictionary<object, int> _processIds = new ConcurrentDictionary<object, int>();

        private readonly string[] YouTubeDlDefaultExeNames = new string[]
        {
            "youtube-dl.exe",
            "youtube-dl",
            "yt-dlp.exe",
            "yt-dlp"
        };

        public YoutubeDlConfig Config { get; set; } = new YoutubeDlConfig();

        public ILogger? Logger { get; set; }

        private ITemporaryFile? _cookieFile;

        private readonly IProcessService _processService;
        private readonly IFileSystem _fileSystem;
        private readonly Regex _outputOutputRegex;

        public YoutubeDlRunner(
            IProcessService processService,
            IFileSystem fileSystem,
            Regex outputOutputRegex)
        {
            _processService = processService;
            _fileSystem = fileSystem;
            _outputOutputRegex = outputOutputRegex;
        }

        /// <summary>
        /// Finds and starts the <c>youtube-dl</c> process.
        /// </summary>
        /// <param name="uri">URI to download from.</param>
        /// <param name="filename">Output filename.</param>
        /// <param name="downloadStatus">Download status.</param>
        /// <param name="configure">Configuration action.</param>
        /// <param name="outputCallback">
        /// Callback on output received.
        /// If it returns <see langword="true"/>, do not do default output processing for that data.
        /// </param>
        /// <param name="errorCallback">
        /// Callback on error received.
        /// If it returns <see langword="true"/>, do not do default error processing for that data.
        /// </param>
        /// <param name="cancellationToken"></param>
        /// <returns>Process.</returns>
        public virtual async Task<IProcess> FindAndStartProcessAsync(
            string uri,
            string filename,
            IDownloadStatus downloadStatus,
            Action<ProcessStartInfo>? configure = null,
            Func<string, IDownloadStatus, bool>? outputCallback = null,
            Func<string, IDownloadStatus, bool>? errorCallback = null,
            CancellationToken cancellationToken = default)
        {
            var startInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
            };

            startInfo.ArgumentList.Add("--retries");
            startInfo.ArgumentList.Add(Config.Retries.ToString());

            startInfo.ArgumentList.Add("--file-access-retries");
            startInfo.ArgumentList.Add(Config.FileAccessRetries.ToString());

            startInfo.ArgumentList.Add("--fragment-retries");
            startInfo.ArgumentList.Add(Config.FragmentRetries.ToString());

            foreach (var retry in Config.RetrySleep)
            {
                startInfo.ArgumentList.Add("--retry-sleep");
                startInfo.ArgumentList.Add(retry);
            }

            startInfo.ArgumentList.Add("--buffer-size");
            startInfo.ArgumentList.Add(Config.BufferSize.ToString());

            startInfo.ArgumentList.Add("--progress");
            startInfo.ArgumentList.Add("--newline");

            if (Config.WriteInfoJson)
            {
                startInfo.ArgumentList.Add("--write-info-json");
            }
            if (Config.WriteSubs)
            {
                startInfo.ArgumentList.Add("--write-subs");
            }

            if (Config.CookieFileContents != null)
            {
                _cookieFile = await CreateCookieFileAsync(
                    Config.CookieFileContents,
                    cancellationToken);

                startInfo.ArgumentList.Add("--cookies");
                startInfo.ArgumentList.Add(_cookieFile.Filename);
            }

            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(filename);

            startInfo.ArgumentList.Add(uri);

            configure?.Invoke(startInfo);

            var process = FindAndStartProcess(startInfo);

            Logger?.LogDebug("Running youtube-dl: {Filename} {ArgumentList}", startInfo.FileName, startInfo.ArgumentList);

            _downloadStatuses[process.Id] = downloadStatus;
            if (outputCallback != null)
            {
                _outputCallbacks[process.Id] = outputCallback;
            }
            if (errorCallback != null)
            {
                _errorCallbacks[process.Id] = errorCallback;
            }

            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        public virtual void Dispose()
        {
            _cookieFile?.Dispose();
        }

        protected virtual async Task<ITemporaryFile> CreateCookieFileAsync(byte[] contents, CancellationToken cancellationToken = default)
        {
            ITemporaryFile? file = null;

            try
            {
                file = new TemporaryFile(_fileSystem);
                await _fileSystem.File.WriteAllBytesAsync(file.Filename, contents, cancellationToken);
                return file;
            }
            catch
            {
                file?.Dispose();
                throw;
            }
        }

        protected virtual void Process_OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data == null)
            {
                return;
            }

            Logger?.LogDebug("YoutubeDl: {Output}", args.Data);

            int processId;
            try
            {
                processId = GetProcessIdBySender(sender);
            }
            catch (Exception exception)
            {
                Logger?.LogError(exception, "YoutubeDl: Could not get process Id.");
                return;
            }

            if (!_downloadStatuses.TryGetValue(processId, out var downloadStatus))
            {
                Logger?.LogError("YoutubeDl: Could not get download status object from process Id {ProcessId}.", processId);
                return;
            }

            downloadStatus.Percentage = null;
            downloadStatus.TotalSize = null;
            downloadStatus.AverageBytesPerSecond = null;
            downloadStatus.EstimatedCompletionTime = null;

            if (_outputCallbacks.TryGetValue(processId, out var callback)
                && callback(args.Data, downloadStatus))
            {
                return;
            }

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
            else if (_outputOutputRegex.TryMatch(args.Data, out var outputMatch))
            {
                downloadStatus.OutputFilename = outputMatch.Groups[OutputOutputRegexGroup].Value;
                downloadStatus.DestinationFilename = downloadStatus.OutputFilename;
            }
            else if (args.Data.StartsWith(OutputDownloadDestination))
            {
                downloadStatus.DestinationFilename = args.Data[OutputDownloadDestination.Length..];
            }
            else if (OutputDownloadProgressShort.TryMatch(args.Data, out var progressShortMatch))
            {
                downloadStatus.AverageBytesPerSecond = SizeToBytes(progressShortMatch.Groups["rate"].Value);
            }
            else if (OutputDownloadProgress.TryMatch(args.Data, out var progressMatch))
            {
                downloadStatus.Percentage = double.Parse(progressMatch.Groups["percent"].Value) / 100;
                downloadStatus.TotalSize = SizeToBytes(progressMatch.Groups["size"].Value);
                downloadStatus.AverageBytesPerSecond = SizeToBytes(progressMatch.Groups["rate"].Value);

                var eta = progressMatch.Groups["eta"].Value;
                if (eta != "Unknown")
                {
                    downloadStatus.EstimatedCompletionTime = ParseTimeSpan(eta);
                }
                else
                {
                    downloadStatus.EstimatedCompletionTime = null;
                }
            }
        }

        protected virtual void Process_ErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data == null)
            {
                return;
            }

            Logger?.LogError("YouTube: error: {Error}", args.Data);

            int processId;
            try
            {
                processId = GetProcessIdBySender(sender);
            }
            catch (Exception exception)
            {
                Logger?.LogError(exception, "YoutubeDl: Could not get process Id.");
                return;
            }

            if (!_downloadStatuses.TryGetValue(processId, out var downloadStatus))
            {
                Logger?.LogError("YoutubeDl: Could not get download status object from process Id {ProcessId}.", processId);
                return;
            }

            if (_errorCallbacks.TryGetValue(processId, out var callback)
                && callback(args.Data, downloadStatus))
            {
                return;
            }
        }

        protected virtual IProcess FindAndStartProcess(ProcessStartInfo startInfo)
        {
            if (Config.ExecutableName != null)
            {
                return StartProcess(Config.ExecutableName.ToString());
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
                startInfo.FileName = executableName;
                return _processService.Start(startInfo)
                    ?? throw new InvalidOperationException($"Could not start process: {startInfo.FileName}");
            }
        }

        protected virtual int GetProcessIdBySender(object sender)
        {
            if (!_processIds.TryGetValue(sender, out var processId))
            {
                // sender could either be a Process or IProcess :(
                processId = (int)sender.GetType().GetProperty("Id").GetValue(sender);
                _processIds[sender] = processId;
            }
            return processId;
        }

        protected virtual long? SizeToBytes(string size)
        {
            if (size.StartsWith("Unknown"))
            {
                return null;
            }

            var match = SizeRegex.Match(size);

            int? multiplier = match.Groups["size"].Value[0] switch
            {
                'B' => 1,
                'K' => 1024,
                'M' => 1024 * 1024,
                'G' => 1024 * 1024 * 1024,
                _ => null,
            };

            if (!multiplier.HasValue)
            {
                return null;
            }

            return (long)(double.Parse(match.Groups["amount"].Value) * multiplier.Value);
        }

        protected virtual TimeSpan ParseTimeSpan(string timeSpan)
        {
            return timeSpan.Count(c => c == ':') == 1
                ? TimeSpan.ParseExact(timeSpan, "mm':'ss", CultureInfo.InvariantCulture)
                : TimeSpan.Parse(timeSpan);
        }
    }
}
