namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// No download filename template given.
    /// </summary>
    public class JobTaskNoDownloadFilenameTemplateException : JobTaskWorkerException
    {
        /// <summary>
        /// No download filename template given.
        /// </summary>
        public JobTaskNoDownloadFilenameTemplateException()
            : base(StalkErrorCodes.JobTaskWorkerNoExtractor) { }
    }
}
