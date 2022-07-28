using System.Runtime.Serialization;

namespace Wilgysef.Stalk.Shared.Enums
{
    public enum JobTaskType
    {
        [EnumMember(Value = "List")]
        List,

        [EnumMember(Value = "Fetch")]
        Fetch,

        [EnumMember(Value = "Download")]
        Download,
    }
}
