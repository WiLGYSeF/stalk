namespace Wilgysef.Stalk.Core.Shared.Enums
{
    public enum JobTaskState
    {
        /// <summary>
        /// Job task is running.
        /// </summary>
        Active,

        /// <summary>
        /// Job task is not running.
        /// </summary>
        Inactive,

        /// <summary>
        /// Job task has completed.
        /// </summary>
        Completed,

        /// <summary>
        /// Job task has failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Job task was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Job task is in process of cancelling.
        /// </summary>
        Cancelling,

        /// <summary>
        /// Job task is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Job task is pausing.
        /// </summary>
        Pausing,
    }
}
