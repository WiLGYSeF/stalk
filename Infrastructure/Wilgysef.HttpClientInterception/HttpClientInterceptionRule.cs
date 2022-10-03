using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wilgysef.HttpClientInterception
{
    public class HttpClientInterceptionRule
    {
        public ICollection<Func<HttpRequestMessage, bool>> RequestFilters => _requestFilters;

        public Func<HttpRequestMessage, HttpResponseMessage>? SendResponseMessage { get; set; }

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? SendResponseMessageAsync { get; set; }

        public Func<HttpRequestMessage, HttpRequestMessage>? ModifyRequest { get; set; }

        public Func<HttpResponseMessage, HttpResponseMessage>? ModifyResponse { get; set; }

        public bool InvokeRequestEvents { get; set; }

        public bool InvokeResponseEvents { get; set; }

        private readonly List<Func<HttpRequestMessage, bool>> _requestFilters = new List<Func<HttpRequestMessage, bool>>();

        public bool IsMatch(HttpRequestMessage request)
        {
            if (RequestFilters.Count == 0)
            {
                return true;
            }

            foreach (var filter in RequestFilters)
            {
                if (!filter(request))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
