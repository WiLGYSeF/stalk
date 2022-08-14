namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// The job is already done.
    /// </summary>
    public class JobAlreadyDoneException : BusinessException
    {
        public JobAlreadyDoneException() : base(StalkErrorCodes.JobAlreadyDone) { }
    }
}
