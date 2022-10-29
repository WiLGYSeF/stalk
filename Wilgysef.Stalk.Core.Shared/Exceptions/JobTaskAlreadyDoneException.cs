namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// The job task is already done.
    /// </summary>
    public class JobTaskAlreadyDoneException : BusinessException
    {
        /// <summary>
        /// The job task is already done.
        /// </summary>
        public JobTaskAlreadyDoneException() : base(StalkErrorCodes.JobTaskAlreadyDone) { }
    }
}
