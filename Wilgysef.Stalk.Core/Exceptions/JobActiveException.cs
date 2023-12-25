using Wilgysef.Core.Exceptions;

namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// The job is active.
    /// </summary>
    public class JobActiveException : BusinessException
    {
        /// <summary>
        /// The job is active.
        /// </summary>
        public JobActiveException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        public override string Code => "JobActive";
    }
}
