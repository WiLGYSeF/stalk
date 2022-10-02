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

        public ICollection<Func<HttpResponseMessage, bool>> ResponseFilters => _responseFilters;

        public Func<HttpRequestMessage, HttpResponseMessage>? SendResponseMessage { get; set; }

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? SendResponseMessageAsync { get; set; }

        public Func<HttpRequestMessage, HttpRequestMessage>? ModifyRequest { get; set; }

        public Func<HttpResponseMessage, HttpResponseMessage>? ModifyResponse { get; set; }

        public bool LogRequest { get; set; }

        public bool LogResponse { get; set; }

        private readonly List<Func<HttpRequestMessage, bool>> _requestFilters = new List<Func<HttpRequestMessage, bool>>();
        private readonly List<Func<HttpResponseMessage, bool>> _responseFilters = new List<Func<HttpResponseMessage, bool>>();

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

        public bool IsMatch(HttpResponseMessage response)
        {
            if (ResponseFilters.Count == 0)
            {
                return true;
            }

            foreach (var filter in ResponseFilters)
            {
                if (!filter(response))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
