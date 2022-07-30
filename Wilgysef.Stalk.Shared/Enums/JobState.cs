using System.Runtime.Serialization;

namespace Wilgysef.Stalk.Shared.Enums
{
    public enum JobState
    {
        /// <summary>
        /// Job is running.
        /// </summary>
        [EnumMember(Value = "Active")]
        Active,

        /// <summary>
        /// Job is not running.
        /// </summary>
        [EnumMember(Value = "Inactive")]
        Inactive,

        /// <summary>
        /// Job has completed.
        /// </summary>
        [EnumMember(Value = "Completed")]
        Completed,

        /// <summary>
        /// Job has failed.
        /// </summary>
        [EnumMember(Value = "Failed")]
        Failed,

        /// <summary>
        /// Job was cancelled.
        /// </summary>
        [EnumMember(Value = "Cancelled")]
        Cancelled,

        /// <summary>
        /// Job is in process of cancelling.
        /// </summary>
        [EnumMember(Value = "Cancelling")]
        Cancelling,
    }
}
