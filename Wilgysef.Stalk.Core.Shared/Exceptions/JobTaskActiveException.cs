namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class JobTaskActiveException : BusinessException
    {
        public JobTaskActiveException() : base(StalkErrorCodes.JobTaskActive) { }
    }
}
