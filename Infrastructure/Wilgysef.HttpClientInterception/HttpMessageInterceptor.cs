using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wilgysef.HttpClientInterception
{
    internal class HttpMessageInterceptor : DelegatingHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage?>> GetResponseAsync { get; set; } = null!;

        public HttpMessageInterceptor(HttpMessageHandler handler) : base(handler) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await GetResponseAsync(request, cancellationToken)
                ?? await base.SendAsync(request, cancellationToken);
        }
    }
}
