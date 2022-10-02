using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wilgysef.HttpClientInterception
{
    internal class HttpMessageInterceptor : DelegatingHandler
    {
        public IDictionary<HttpRequestMessage, Func<HttpRequestMessage, HttpResponseMessage>>? ResponseFuncs { get; set; }

        public IDictionary<HttpRequestMessage, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>? ResponseAsyncFuncs { get; set; }

        public HttpMessageInterceptor(HttpMessageHandler handler) : base(handler) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ResponseAsyncFuncs?.TryGetValue(request, out var responseFuncAsync) ?? false)
            {
                ResponseAsyncFuncs.Remove(request);
                return await responseFuncAsync(request, cancellationToken);
            }
            if (ResponseFuncs?.TryGetValue(request, out var responseFunc) ?? false)
            {
                ResponseFuncs.Remove(request);
                return responseFunc(request);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
