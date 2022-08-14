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

        #endregion
    }
}
