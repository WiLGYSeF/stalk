namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// The job task is active.
    /// </summary>
    public class JobTaskActiveException : BusinessException
    {
        public JobTaskActiveException() : base(StalkErrorCodes.JobTaskActive) { }
    }
}
