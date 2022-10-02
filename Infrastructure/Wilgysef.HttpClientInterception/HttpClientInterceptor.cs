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

        public event EventHandler<Exception>? ErrorOccurred;

        public bool ThrowOnError { get; set; } = true;

        private readonly List<HttpClientInterceptionRule> _rules = new List<HttpClientInterceptionRule>();
        private readonly ConcurrentDictionary<HttpRequestMessage, HttpRequestMatch> _requestMatches = new ConcurrentDictionary<HttpRequestMessage, HttpRequestMatch>();

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

            messageInterceptor.GetResponseAsync = interceptor.GetResponseMessage;
    
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

        public async Task<HttpResponseMessage?> GetResponseMessage(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_requestMatches.TryGetValue(request, out var match))
            {
                if (match.ResponseAsyncFunc != null)
                {
                    return await match.ResponseAsyncFunc(request, cancellationToken);
                }
                if (match.ResponseFunc != null)
                {
                    return match.ResponseFunc(request);
                }
            }
            return null;
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Func<HttpRequestMessage, HttpResponseMessage>? responseFunc = null;
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? responseAsyncFunc = null;
            var matchingRules = new List<HttpClientInterceptionRule>();
            var logRequest = LogRequestAlways;

            for (var i = _rules.Count - 1; i >= 0; i--)
            {
                var rule = _rules[i];

                try
                {
                    if (rule.IsMatch(request))
                    {
                        matchingRules.Add(rule);
                        logRequest |= rule.LogRequest;

                        if (responseFunc == null && responseAsyncFunc == null)
                        {
                            responseFunc = rule.SendResponseMessage;
                            responseAsyncFunc = rule.SendResponseMessageAsync;
                        }

                        if (rule.ModifyRequest != null)
                        {
                            request = rule.ModifyRequest(request);
                        }
                    }
                }
                catch (Exception exception)
                {
                    ErrorOccurred?.Invoke(this, exception);

                    if (ThrowOnError)
                    {
                        throw;
                    }
                }
            }

            _requestMatches.TryAdd(
                request,
                new HttpRequestMatch(
                    request,
                    responseFunc,
                    responseAsyncFunc,
                    matchingRules));

            if (logRequest)
            {
                LogRequest(request);
            }

            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var logResponse = LogResponseAlways;

            if (response.RequestMessage != null && _requestMatches.TryGetValue(response.RequestMessage, out var match))
            {
                foreach (var rule in match.MatchingRules)
                {
                    logResponse |= rule.LogResponse;
                    if (rule.ModifyResponse != null)
                    {
                        try
                        {
                            response = rule.ModifyResponse(response);
                        }
                        catch (Exception exception)
                        {
                            ErrorOccurred?.Invoke(this, exception);

                            if (ThrowOnError)
                            {
                                throw;
                            }
                        }
                    }
                }
                _requestMatches.Remove(response.RequestMessage, out _);
            }

            if (logResponse)
            {
                LogResponse(response);
            }

            return response;
        }

        private void LogRequest(HttpRequestMessage request)
        {
            try
            {
                LogRequestAction?.Invoke(request);
            }
            catch (Exception exception)
            {
                ErrorOccurred?.Invoke(this, exception);

                if (ThrowOnError)
                {
                    throw;
                }
            }
        }

        private void LogResponse(HttpResponseMessage response)
        {
            try
            {
                LogResponseAction?.Invoke(response);
            }
            catch (Exception exception)
            {
                ErrorOccurred?.Invoke(this, exception);

                if (ThrowOnError)
                {
                    throw;
                }
            }
        }
    }
}
