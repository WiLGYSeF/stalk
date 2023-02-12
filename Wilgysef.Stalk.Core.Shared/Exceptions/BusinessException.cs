using System;

namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// Business exception.
    /// </summary>
    public abstract class BusinessException : Exception
    {
        /// <summary>
        /// Business error code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Business exception.
        /// </summary>
        /// <param name="code">Business error code.</param>
        /// <param name="message">Message.</param>
        /// <param name="innerException">Inner exception.</param>
        public BusinessException(string code, string? message = null, Exception? innerException = null)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}
