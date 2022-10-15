using System;

namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// Job task worker exception.
    /// </summary>
    public class JobTaskWorkerException : Exception
    {
        /// <summary>
        /// Error code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Job task worker exception.
        /// </summary>
        /// <param name="code">Error code.</param>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public JobTaskWorkerException(
            string code,
            string? message = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}
