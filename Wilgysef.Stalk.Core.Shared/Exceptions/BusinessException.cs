using System;

namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class BusinessException : Exception
    {
        /// <summary>
        /// Business error code.
        /// </summary>
        public string Code { get; private set; }

        public BusinessException(string code)
        {
            Code = code;
        }
    }
}
