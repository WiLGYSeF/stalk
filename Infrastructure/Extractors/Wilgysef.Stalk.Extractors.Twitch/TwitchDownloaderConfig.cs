﻿using System.Linq.Expressions;
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
    public static readonly string CookieFileContentsKey = "cookieFileContents";

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

    /// <summary>
    /// Whether <c>*.info.json</c> file contents be moved to the metadata file.
    /// </summary>
    public bool MoveInfoJsonToMetadata { get; set; } = false;

    /// <summary>
    /// <c>youtube-dl</c> executable path.
    /// </summary>
    public string? ExecutableName { get; set; }

    /// <summary>
    /// Cookie file contents encoded in base64.
    /// </summary>
    public string? CookieFileContents { get; set; }

    public TwitchDownloaderConfig() { }

    public TwitchDownloaderConfig(IDictionary<string, object?>? config)
    {
        TrySetValue(() => Retries, config, RetriesKey);
        TrySetValue(() => FileAccessRetries, config, FileAccessRetriesKey);
        TrySetValue(() => FragmentRetries, config, FragmentRetriesKey);

        if (config?.TryGetValueAs<IEnumerable<string>, string, object?>(RetrySleepKey, out var retrySleep) ?? false)
        {
            RetrySleep.AddRange(retrySleep);
        }

        TrySetValue(() => BufferSize, config, BufferSizeKey);
        TrySetValue(() => WriteInfoJson, config, WriteInfoJsonKey);
        TrySetValue(() => WriteSubs, config, WriteSubsKey);
        TrySetValue(() => MoveInfoJsonToMetadata, config, MoveInfoJsonToMetadataKey);
        TrySetValue(() => ExecutableName, config, ExecutableNameKey);
        TrySetValue(() => CookieFileContents, config, CookieFileContentsKey);
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
            CookieFileContents = CookieFileContents != null
                ? Convert.FromBase64String(CookieFileContents)
                : null,
        };
    }

    // TODO: move to shared
    private static bool TrySetValue<T>(Expression<Func<T>> property, IDictionary<string, object?>? config, string key)
    {
        if (!(config?.TryGetValueAs<T, string, object?>(key, out var value) ?? false))
        {
            return false;
        }

        Expression.Lambda(Expression.Assign(property.Body, Expression.Constant(value)))
            .Compile()
            .DynamicInvoke();
        return true;
    }
}
