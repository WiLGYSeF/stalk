using Wilgysef.Core.Exceptions;

namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// The job is already done.
    /// </summary>
    public class JobAlreadyDoneException : BusinessException
    {
        /// <summary>
        /// The job is already done.
        /// </summary>
        public JobAlreadyDoneException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        public override string Code => "JobAlreadyDone";
    }
}
