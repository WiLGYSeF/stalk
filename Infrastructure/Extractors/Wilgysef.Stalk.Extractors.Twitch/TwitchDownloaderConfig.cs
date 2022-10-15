using Wilgysef.Stalk.Core.Shared.Extensions;
using Wilgysef.Stalk.Extractors.YoutubeDl.Core;

namespace Wilgysef.Stalk.Extractors.Twitch;

public class TwitchDownloaderConfig
{
    public static readonly string RetriesKey = "retries";
    public static readonly string FileAccessRetriesKey = "fileAccessRetries";
    public static readonly string FragmentRetriesKey = "fragmentRetries";
    public static readonly string RetrySleepKey = "retrySleep";
    public static readonly string BufferSizeKey = "bufferSize";
    public static readonly string WriteInfoJsonKey = "writeInfoJson";
    public static readonly string WriteSubsKey = "writeSubs";
    public static readonly string MoveInfoJsonToMetadataKey = "moveInfoJsonToMetadata";
    public static readonly string ExecutableNameKey = "executableName";

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

    public bool MoveInfoJsonToMetadata { get; set; } = false;

    public string? ExecutableName { get; set; }

    public TwitchDownloaderConfig() { }

    public TwitchDownloaderConfig(IDictionary<string, object?>? config)
    {
        if (config?.TryGetValueAs<int, string, object?>(RetriesKey, out var retries) ?? false)
        {
            Retries = retries;
        }
        if (config?.TryGetValueAs<int, string, object?>(FileAccessRetriesKey, out var fileAccessRetries) ?? false)
        {
            FileAccessRetries = fileAccessRetries;
        }
        if (config?.TryGetValueAs<int, string, object?>(FragmentRetriesKey, out var fragmentRetries) ?? false)
        {
            FragmentRetries = fragmentRetries;
        }
        if (config?.TryGetValueAs<IEnumerable<string>, string, object?>(RetrySleepKey, out var retrySleep) ?? false)
        {
            RetrySleep.AddRange(retrySleep);
        }
        if (config?.TryGetValueAs<int, string, object?>(BufferSizeKey, out var bufferSize) ?? false)
        {
            BufferSize = bufferSize;
        }
        if (config?.TryGetValueAs<bool, string, object?>(WriteInfoJsonKey, out var writeInfoJson) ?? false)
        {
            WriteInfoJson = writeInfoJson;
        }
        if (config?.TryGetValueAs<bool, string, object?>(WriteSubsKey, out var writeSubs) ?? false)
        {
            WriteSubs = writeSubs;
        }
        if (config?.TryGetValueAs<bool, string, object?>(MoveInfoJsonToMetadataKey, out var moveInfoJsonToMetadata) ?? false)
        {
            MoveInfoJsonToMetadata = moveInfoJsonToMetadata;
        }
        if (config?.TryGetValueAs<string, string, object?>(ExecutableNameKey, out var executableName) ?? false)
        {
            ExecutableName = executableName;
        }
    }

    public YoutubeDlConfig ToYoutubeDlConfig()
    {
        return new YoutubeDlConfig
        {
            Retries = Retries,
            FileAccessRetries = FileAccessRetries,
            FragmentRetries = FragmentRetries,
            RetrySleep = RetrySleep,
            BufferSize = BufferSize,
            WriteInfoJson = WriteInfoJson,
            WriteSubs = WriteSubs,
            MoveInfoJsonToMetadata = MoveInfoJsonToMetadata,
            ExecutableName = ExecutableName,
        };
    }
}
