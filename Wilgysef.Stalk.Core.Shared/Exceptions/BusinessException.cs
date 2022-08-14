using System;

namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// Business exception.
    /// </summary>
    public class BusinessException : Exception
    {
        /// <summary>
        /// Business error code.
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Business exception.
        /// </summary>
        public BusinessException(string code)
        {
            Code = code;
        }
    }
}
