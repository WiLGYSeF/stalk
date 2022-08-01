namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class JobNotPausedException : BusinessException
    {
        public JobNotPausedException() : base(StalkErrorCodes.JobNotPaused) { }
    }
}
