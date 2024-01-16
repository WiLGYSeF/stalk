using Wilgysef.Core.Exceptions;

namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// The job task is already done.
    /// </summary>
    public class JobTaskAlreadyDoneException : BusinessException
    {
        /// <summary>
        /// The job task is already done.
        /// </summary>
        public JobTaskAlreadyDoneException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        public override string Code => "JobTaskAlreadyDone";
    }
}
