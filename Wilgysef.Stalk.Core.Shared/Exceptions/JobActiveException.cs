namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class JobActiveException : BusinessException
    {
        public JobActiveException() : base(StalkErrorCodes.JobActive) { }
    }
}
