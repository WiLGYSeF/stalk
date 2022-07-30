using System.Runtime.Serialization;

namespace Wilgysef.Stalk.Core.Shared.Enums
{
    public enum JobTaskState
    {
        /// <summary>
        /// Job task is running.
        /// </summary>
        [EnumMember(Value = "Active")]
        Active,

        /// <summary>
        /// Job task is not running.
        /// </summary>
        [EnumMember(Value = "Inactive")]
        Inactive,

        /// <summary>
        /// Job task has completed.
        /// </summary>
        [EnumMember(Value = "Completed")]
        Completed,

        /// <summary>
        /// Job task has failed.
        /// </summary>
        [EnumMember(Value = "Failed")]
        Failed,

        /// <summary>
        /// Job task was cancelled.
        /// </summary>
        [EnumMember(Value = "Cancelled")]
        Cancelled,

        /// <summary>
        /// Job task is in process of cancelling.
        /// </summary>
        [EnumMember(Value = "Cancelling")]
        Cancelling,
    }
}
