using System;
using System.Net.Http;
using System.Reflection;

namespace Wilgysef.Stalk.TestBase.Shared
{
    public static class HttpUtilities
    {
        public static HttpResponseMessage GetResponseMessageFromManifestResource(string name)
        {
            var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(name);
            if (stream == null)
            {
                throw new ArgumentException($"Assembly manifest resouce was not found for {name}", nameof(name));
            }

            return new HttpResponseMessage()
            {
                Content = new StreamContent(stream),
            };
        }
    }
}
