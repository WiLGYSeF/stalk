using System;

namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public abstract class JobTaskWorkerException : BusinessException
    {
        protected JobTaskWorkerException(string code, string? message = null, Exception? innerException = null)
            : base(code, message, innerException) { }
    }
}
