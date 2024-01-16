using System;

namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// No extractor could be found to extract from the URI.
    /// </summary>
    public class JobTaskNoExtractorException : JobTaskWorkerException
    {
        public static readonly string ErrorCode = "JobTaskWorkerNoExtractor";

        public Uri Uri { get; }

        /// <summary>
        /// No extractor could be found to extract from the URI.
        /// </summary>
        /// <param name="uri">URI without extractor.</param>
        public JobTaskNoExtractorException(Uri uri)
        {
            Uri = uri;
        }

        public override string Code => ErrorCode;
    }
}
