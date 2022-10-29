using System;
using System.Collections.Generic;
using System.Text;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public class YoutubeDlConfig
    {
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
        public List<string> RetrySleep { get; set; } = new List<string>();

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
        /// Cookie file contents.
        /// </summary>
        public byte[]? CookieFileContents { get; set; }
    }
}
