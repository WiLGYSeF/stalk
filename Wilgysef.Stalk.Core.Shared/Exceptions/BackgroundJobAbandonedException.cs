namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class BackgroundJobAbandonedException : BusinessException
    {
        public BackgroundJobAbandonedException() : base(StalkErrorCodes.BackgroundJobAbandoned) { }
    }
}
