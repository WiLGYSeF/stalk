namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// The job task is already transitioning to a different state.
    /// </summary>
    public class JobTaskTransitioningException : BusinessException
    {
        /// <summary>
        /// The job task is already transitioning to a different state.
        /// </summary>
        public JobTaskTransitioningException() : base(StalkErrorCodes.JobTaskTransitioning) { }
    }
}
