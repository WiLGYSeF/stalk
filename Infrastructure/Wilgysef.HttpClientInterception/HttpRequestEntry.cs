using System;
using System.Net.Http;

namespace Wilgysef.HttpClientInterception
{
    public class HttpRequestEntry
    {
        public HttpRequestMessage Request { get; }

        public DateTimeOffset RequestTime { get; }

        public HttpResponseMessage? Response { get; private set; }

        public DateTimeOffset? ResponseTime { get; private set; }

        public HttpRequestEntry(
            HttpRequestMessage request,
            DateTimeOffset requestTime,
            HttpResponseMessage? response,
            DateTimeOffset? responseTime)
        {
            Request = request;
            RequestTime = requestTime;
            Response = response;
            ResponseTime = responseTime;
        }

        internal void SetResponse(HttpResponseMessage response, DateTimeOffset responseTime)
        {
            Response = response;
            ResponseTime = responseTime;
        }
    }
}
