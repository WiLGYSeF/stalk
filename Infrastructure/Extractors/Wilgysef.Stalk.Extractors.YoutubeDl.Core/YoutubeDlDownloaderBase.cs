using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.ProcessServices;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public abstract class YoutubeDlDownloaderBase : DownloaderBase
    {
        protected IProcessService ProcessService { get; }

        protected abstract Regex OutputOutputRegex { get; }

        protected YoutubeDlDownloaderBase(
            IFileSystem fileSystem,
            IStringFormatter stringFormatter,
            IFilenameSlugSelector filenameSlugSelector,
            IMetadataSerializer metadataSerializer,
            IProcessService processService,
            HttpClient httpClient)
            : base(fileSystem, stringFormatter, filenameSlugSelector, metadataSerializer, httpClient)
        {
            ProcessService = processService;
        }

        protected abstract YoutubeDlConfig GetYoutubeDlConfig();

        protected abstract bool ShouldMoveInfoJsonToMetadata();

        protected virtual YoutubeDlRunner GetYoutubeDlRunner()
        {
            return new YoutubeDlRunner(ProcessService, FileSystem, OutputOutputRegex)
            {
                Config = GetYoutubeDlConfig(),
                Logger = Logger,
            };
        }

        protected override async IAsyncEnumerable<DownloadFileResult> SaveFileAsync(
            Uri uri,
            string filenameTemplate,
            IMetadataObject metadata,
            DownloadRequestData? requestData = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var youtubeDl = GetYoutubeDlRunner();

            var filename = GetFormattedSlugifiedFilename(filenameTemplate, metadata);

            CreateDirectoriesFromFilename(filename);

            var status = new DownloadStatus();
            var process = await youtubeDl.FindAndStartProcessAsync(
                uri.AbsoluteUri,
                filename,
                status,
                cancellationToken: cancellationToken);

            await WaitForExitOrKillAsync(process, cancellationToken);

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
                if (ShouldMoveInfoJsonToMetadata())
                {
                    try
                    {
                        await MoveInfoJsonToMetadataAsync(status.MetadataFilename, metadata);
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

        private async Task MoveInfoJsonToMetadataAsync(string metadataFilename, IMetadataObject metadata)
        {
            using (var stream = FileSystem.File.Open(metadataFilename, FileMode.Open))
            {
                using var reader = new StreamReader(stream);
                var youtubeMetadata = JsonSerializer.Deserialize<IDictionary<string, object>>(await reader.ReadToEndAsync());
                metadata["youtube_dl"] = youtubeMetadata;
            }

            FileSystem.File.Delete(metadataFilename);
        }

        private static async Task WaitForExitOrKillAsync(IProcess process, CancellationToken cancellationToken = default)
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                throw;
            }
        }

        private string GetFullPath(IProcess process, string path)
        {
            return FileSystem.Path.GetFullPath(
                FileSystem.Path.Combine(process.StartInfo.WorkingDirectory, path));
        }
    }
}
