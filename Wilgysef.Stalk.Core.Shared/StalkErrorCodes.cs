namespace Wilgysef.Stalk.Core.Shared
{
    public static class StalkErrorCodes
    {
        #region Job Error Codes

        /// <summary>
        /// The job is already done.
        /// </summary>
        public const string JobAlreadyDone = "Stalk:000001";

        /// <summary>
        /// The job is active.
        /// </summary>
        public const string JobActive = "Stalk:000002";

        /// <summary>
        /// The job is already transitioning to a different state.
        /// </summary>
        public const string JobTransitioning = "Stalk:000003";

        #endregion

        #region Job Task Error Codes

        /// <summary>
        /// The job task is already done.
        /// </summary>
        public const string JobTaskAlreadyDone = "Stalk:001001";

        /// <summary>
        /// The job task is active.
        /// </summary>
        public const string JobTaskActive = "Stalk:001002";

        /// <summary>
        /// The job task is already transitioning to a different state.
        /// </summary>
        public const string JobTaskTransitioning = "Stalk:001003";

        #endregion

        #region Job Worker Error Codes

        #endregion

        #region Job Task Worker Error Codes

        /// <summary>
        /// No extractor could be found to extract from the URI.
        /// </summary>
        public const string JobTaskWorkerNoExtractor = "Stalk:003001";

        /// <summary>
        /// No download filename template given.
        /// </summary>
        public const string JobTaskWorkerNoDownloadFilenameTemplate = "Stalk:003002";

        #endregion
    }
}
