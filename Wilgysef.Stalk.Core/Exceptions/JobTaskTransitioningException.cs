using Wilgysef.Core.Exceptions;

namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// The job task is already transitioning to a different state.
    /// </summary>
    public class JobTaskTransitioningException : BusinessException
    {
        /// <summary>
        /// The job task is already transitioning to a different state.
        /// </summary>
        public JobTaskTransitioningException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        public override string Code => "JobTaskTransitioning";
    }
}
