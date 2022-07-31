namespace Wilgysef.Stalk.Core.Shared.Enums
{
    public enum JobState
    {
        /// <summary>
        /// Job is running.
        /// </summary>
        Active,

        /// <summary>
        /// Job is not running.
        /// </summary>
        Inactive,

        /// <summary>
        /// Job has completed.
        /// </summary>
        Completed,

        /// <summary>
        /// Job has failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Job was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Job is in process of cancelling.
        /// </summary>
        Cancelling,

        /// <summary>
        /// Job is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Job is pausing.
        /// </summary>
        Pausing,
    }
}
