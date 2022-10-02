using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Wilgysef.HttpClientInterception
{
    public class HttpClientInterceptor : MessageProcessingHandler
    {
        public List<HttpClientInterceptionRule> Rules => _rules;

        public Action<HttpRequestMessage>? LogRequestAction { get; set; }

        public Action<HttpResponseMessage>? LogResponseAction { get; set; }

        public bool LogRequestAlways { get; set; }

        public bool LogResponseAlways { get; set; }

        private readonly List<HttpClientInterceptionRule> _rules = new List<HttpClientInterceptionRule>();
        private readonly ConcurrentDictionary<HttpRequestMessage, Func<HttpRequestMessage, HttpResponseMessage>> _responseFuncs = new ConcurrentDictionary<HttpRequestMessage, Func<HttpRequestMessage, HttpResponseMessage>>();
        private readonly ConcurrentDictionary<HttpRequestMessage, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responseAsyncFuncs = new ConcurrentDictionary<HttpRequestMessage, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();

        private readonly HttpMessageInterceptor _messageInterceptor;

        private HttpClientInterceptor(HttpMessageInterceptor interceptor) : base(interceptor)
        {
            _messageInterceptor = interceptor;
        }

        public static HttpClientInterceptor Create()
        {
            return Create(new HttpClientHandler());
        }

        public static HttpClientInterceptor Create(HttpMessageHandler innerHandler)
        {
            var messageInterceptor = new HttpMessageInterceptor(innerHandler);
            var interceptor = new HttpClientInterceptor(messageInterceptor);

            messageInterceptor.ResponseFuncs = interceptor._responseFuncs;
            messageInterceptor.ResponseAsyncFuncs = interceptor._responseAsyncFuncs;
    
            return interceptor;
        }

        public HttpClientInterceptor AddRule(HttpClientInterceptionRule rule)
        {
            _rules.Add(rule);
            return this;
        }

        public HttpClientInterceptor AddUri(Uri uri, Func<HttpRequestMessage, HttpResponseMessage> action)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(uri).RespondWith(action).Create());
        }

        public HttpClientInterceptor AddUri(string uri, Func<HttpRequestMessage, HttpResponseMessage> action)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(uri).RespondWith(action).Create());
        }

        public HttpClientInterceptor AddUri(Regex regex, Func<HttpRequestMessage, HttpResponseMessage> action)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(regex).RespondWith(action).Create());
        }

        public HttpClientInterceptor AddUri(Uri uri, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(uri).RespondWith(action).Create());
        }

        public HttpClientInterceptor AddUri(string uri, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(uri).RespondWith(action).Create());
        }

        public HttpClientInterceptor AddUri(Regex regex, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(regex).RespondWith(action).Create());
        }

        public HttpClientInterceptor LogRequests(Action<HttpRequestMessage> action)
        {
            LogRequestAction = action;
            LogRequestAlways = false;
            return this;
        }

        public HttpClientInterceptor LogResponses(Action<HttpResponseMessage> action)
        {
            LogResponseAction = action;
            LogResponseAlways = false;
            return this;
        }

        public HttpClientInterceptor LogRequestsAlways(Action<HttpRequestMessage> action)
        {
            LogRequestAction = action;
            LogRequestAlways = true;
            return this;
        }

        public HttpClientInterceptor LogResponsesAlways(Action<HttpResponseMessage> action)
        {
            LogResponseAction = action;
            LogResponseAlways = true;
            return this;
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var logRequest = LogRequestAlways;

            for (var i = _rules.Count - 1; i >= 0; i--)
            {
                var rule = _rules[i];
                if (rule.IsMatch(request))
                {
                    logRequest |= rule.LogRequest;

                    if (rule.SendResponseMessageAsync != null)
                    {
                        _responseAsyncFuncs[request] = rule.SendResponseMessageAsync;
                        break;
                    }
                    if (rule.SendResponseMessage != null)
                    {
                        _responseFuncs[request] = rule.SendResponseMessage;
                        break;
                    }

                    if (rule.ModifyRequest != null)
                    {
                        request = rule.ModifyRequest(request);
                    }
                }
            }

            if (logRequest)
            {
                LogRequestAction?.Invoke(request);
            }

            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var logResponse = LogResponseAlways;

            for (var i = _rules.Count - 1; i >= 0; i--)
            {
                var rule = _rules[i];
                if (rule.IsMatch(response))
                {
                    logResponse |= rule.LogResponse;

                    if (rule.ModifyResponse != null)
                    {
                        response = rule.ModifyResponse(response);
                    }
                }
            }

            if (logResponse)
            {
                LogResponseAction?.Invoke(response);
            }

            return response;
        }
    }
}
