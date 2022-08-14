namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// The job is active.
    /// </summary>
    public class JobActiveException : BusinessException
    {
        public JobActiveException() : base(StalkErrorCodes.JobActive) { }
    }
}
