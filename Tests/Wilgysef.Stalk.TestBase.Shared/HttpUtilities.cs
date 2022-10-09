using System;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace Wilgysef.Stalk.TestBase.Shared
{
    public static class HttpUtilities
    {
        public static HttpResponseMessage GetResponseMessageFromManifestResource(
            string name,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                throw new ArgumentException($"Assembly manifest resouce was not found for {name}", nameof(name));
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = new StreamContent(stream),
            };
        }
    }
}
