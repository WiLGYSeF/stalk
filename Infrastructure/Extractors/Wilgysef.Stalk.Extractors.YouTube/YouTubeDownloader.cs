using System.Diagnostics;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.FileServices;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

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
    public string[] RetrySleep { get; set; }

    /// <summary>
    /// <c>--buffer-size</c>
    /// </summary>
    public int BufferSize { get; set; }

    /// <summary>
    /// <c>--write-info-json</c>
    /// </summary>
    public bool WriteInfoJson { get; set; } = true;

    /// <summary>
    /// <c>--write-subs</c>
    /// </summary>
    public bool WriteSubs { get; set; } = true;

    private readonly IStringFormatter _stringFormatter;
    private readonly IFilenameSlugSelector _filenameSlugSelector;

    public YouTubeDownloader(
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
    {
        _stringFormatter = stringFormatter;
        _filenameSlugSelector = filenameSlugSelector;
    }

    public override bool CanDownload(Uri uri)
    {
        return Consts.VideoRegex.IsMatch(uri.AbsoluteUri);
    }

    protected override async Task<DownloadFileResult> SaveFileAsync(
        Uri uri,
        string filenameTemplate,
        IMetadataObject metadata,
        DownloadRequestData? requestData = null,
        CancellationToken cancellationToken = default)
    {
        // --cookies FILE

        var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
        var filename = filenameSlug.SlugifyPath(
            _stringFormatter.Format(filenameTemplate, metadata.GetFlattenedDictionary()));

        var youtubeDlFilename = "";

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

        var process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException($"Could not start process: {processStartInfo.FileName}");
        process.OutputDataReceived += OutputReceivedHandler;
        process.ErrorDataReceived += ErrorReceivedHandler;

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            Debug.WriteLine($"Cancelled {process.ExitCode} {process.HasExited}");
            throw;
        }

        return new DownloadFileResult(
            "a",
            1,
            "b",
            "3");
    }

    private void OutputReceivedHandler(object sender, DataReceivedEventArgs args)
    {
        Debug.WriteLine(args.Data);
    }

    private void ErrorReceivedHandler(object sender, DataReceivedEventArgs args)
    {
        Debug.WriteLine(args.Data);
    }
}
