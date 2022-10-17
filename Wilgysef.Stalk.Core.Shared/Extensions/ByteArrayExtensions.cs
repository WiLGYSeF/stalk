using System.Text;

namespace Wilgysef.Stalk.Core.Shared.Extensions
{
    public static class ByteArrayExtensions
    {
        private const string HexAlphabetLower = "0123456789abcdef";
        private const string HexAlphabetUpper = "0123456789ABCDEF";

        public static string ToHexString(this byte[] bytes, bool uppercase = false)
        {
            var result = new StringBuilder(bytes.Length * 2);
            var hex = uppercase ? HexAlphabetUpper : HexAlphabetLower;

            foreach (var b in bytes)
            {
                result.Append(hex[b >> 4]);
                result.Append(hex[b & 15]);
            }
            return result.ToString();
        }
    }
}
