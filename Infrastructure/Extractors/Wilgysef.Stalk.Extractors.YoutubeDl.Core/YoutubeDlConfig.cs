using System;
using System.Collections.Generic;

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

        public bool MoveInfoJsonToMetadata { get; set; } = false;

        public string? ExecutableName { get; set; }

        public string? CookieString { get; set; }
    }
}
