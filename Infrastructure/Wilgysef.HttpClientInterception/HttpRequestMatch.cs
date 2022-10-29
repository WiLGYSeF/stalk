using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wilgysef.HttpClientInterception
{
    internal class HttpRequestMatch
    {
        public HttpRequestMessage Request { get; }

        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFunc { get; }

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? ResponseAsyncFunc { get; }

        public IList<HttpClientInterceptionRule> MatchingRules { get; }

        public HttpRequestMatch(
            HttpRequestMessage request,
            Func<HttpRequestMessage, HttpResponseMessage>? responseFunc,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? responseAsyncFunc,
            IList<HttpClientInterceptionRule> matchingRules)
        {
            Request = request;
            ResponseFunc = responseFunc;
            ResponseAsyncFunc = responseAsyncFunc;
            MatchingRules = matchingRules;
        }
    }
}
