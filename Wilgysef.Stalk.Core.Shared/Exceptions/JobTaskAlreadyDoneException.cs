namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class JobTaskAlreadyDoneException : BusinessException
    {
        public JobTaskAlreadyDoneException() : base(StalkErrorCodes.JobTaskAlreadyDone) { }
    }
}
