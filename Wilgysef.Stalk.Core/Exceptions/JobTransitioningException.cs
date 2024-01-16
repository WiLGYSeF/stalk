using Wilgysef.Core.Exceptions;

namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// The job is already transitioning to a different state.
    /// </summary>
    public class JobTransitioningException : BusinessException
    {
        /// <summary>
        /// The job is already transitioning to a different state.
        /// </summary>
        public JobTransitioningException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        public override string Code => "JobTaskTransitioning";
    }
}
