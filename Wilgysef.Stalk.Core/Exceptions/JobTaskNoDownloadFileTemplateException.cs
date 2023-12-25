namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// No download filename template given.
    /// </summary>
    public class JobTaskNoDownloadFilenameTemplateException : JobTaskWorkerException
    {
        /// <summary>
        /// No download filename template given.
        /// </summary>
        public JobTaskNoDownloadFilenameTemplateException() { }

        public override string Code => "JobTaskWorkerNoExtractor";
    }
}
