using Wilgysef.Core.Exceptions;

namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// The job task is active.
    /// </summary>
    public class JobTaskActiveException : BusinessException
    {
        /// <summary>
        /// The job task is active.
        /// </summary>
        public JobTaskActiveException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        public override string Code => "JobTaskActive";
    }
}
