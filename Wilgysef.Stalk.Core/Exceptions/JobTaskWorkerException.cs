using System;
using Wilgysef.Core.Exceptions;

namespace Wilgysef.Stalk.Core.Exceptions
{
    public abstract class JobTaskWorkerException : BusinessException
    {
        protected JobTaskWorkerException(string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
