namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class JobTaskNotPausedException : BusinessException
    {
        public JobTaskNotPausedException() : base(StalkErrorCodes.JobTaskNotPaused) { }
    }
}
