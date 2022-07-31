namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class JobAlreadyDoneException : BusinessException
    {
        public JobAlreadyDoneException() : base(StalkErrorCodes.JobAlreadyDone) { }
    }
}
