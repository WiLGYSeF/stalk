namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// The job is already transitioning to a different state.
    /// </summary>
    public class JobTransitioningException : BusinessException
    {
        /// <summary>
        /// The job is already transitioning to a different state.
        /// </summary>
        public JobTransitioningException() : base(StalkErrorCodes.JobTransitioning) { }
    }
}
