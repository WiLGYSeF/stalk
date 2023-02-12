using System;

namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// No extractor could be found to extract from the URI.
    /// </summary>
    public class JobTaskNoExtractorException : JobTaskWorkerException
    {
        public Uri Uri { get; }

        /// <summary>
        /// No extractor could be found to extract from the URI.
        /// </summary>
        /// <param name="uri">URI without extractor.</param>
        public JobTaskNoExtractorException(Uri uri)
            : base(StalkErrorCodes.JobTaskWorkerNoExtractor)
        {
            Uri = uri;
        }
    }
}
