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
        // TODO: RemoveRule for Uri, string, Regex

        public List<HttpClientInterceptionRule> Rules => _rules;

        public event EventHandler<HttpRequestMessage>? RequestProcessed;

        public event EventHandler<HttpResponseMessage>? ResponseReceived;

        public event EventHandler<Exception>? ErrorOccurred;

        public bool InvokeRequestEventsAlways { get; set; }

        public bool InvokeResponseEventsAlways { get; set; }

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

        public HttpClientInterceptor AddRule(HttpClientInterceptionRule rule, int index)
        {
            _rules.Insert(index, rule);
            return this;
        }

        public bool RemoveRule(HttpClientInterceptionRule rule)
        {
            return _rules.Remove(rule);
        }

        public void ClearRules()
        {
            _rules.Clear();
        }

        #region AddUri

        public HttpClientInterceptor AddUri(Uri uri, Func<HttpRequestMessage, HttpResponseMessage> action, bool invokeEvents = true)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(uri).RespondWith(action).InvokeEvents(invokeEvents).Create());
        }

        public HttpClientInterceptor AddUri(string uri, Func<HttpRequestMessage, HttpResponseMessage> action, bool invokeEvents = true)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(uri).RespondWith(action).InvokeEvents(invokeEvents).Create());
        }

        public HttpClientInterceptor AddUri(Regex regex, Func<HttpRequestMessage, HttpResponseMessage> action, bool invokeEvents = true)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(regex).RespondWith(action).InvokeEvents(invokeEvents).Create());
        }

        public HttpClientInterceptor AddUri(Uri uri, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action, bool invokeEvents = true)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(uri).RespondWith(action).InvokeEvents(invokeEvents).Create());
        }

        public HttpClientInterceptor AddUri(string uri, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action, bool invokeEvents = true)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(uri).RespondWith(action).InvokeEvents(invokeEvents).Create());
        }

        public HttpClientInterceptor AddUri(Regex regex, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action, bool invokeEvents = true)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().ForUri(regex).RespondWith(action).InvokeEvents(invokeEvents).Create());
        }

        #endregion

        public HttpClientInterceptor AddForAny(Func<HttpRequestMessage, HttpResponseMessage> action, bool invokeEvents = false)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().RespondWith(action).InvokeEvents(invokeEvents).Create());
        }

        public HttpClientInterceptor AddForAny(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action, bool invokeEvents = false)
        {
            return AddRule(new HttpClientInterceptionRuleBuilder().RespondWith(action).InvokeEvents(invokeEvents).Create());
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
            var invokeEvent = InvokeRequestEventsAlways;

            for (var i = _rules.Count - 1; i >= 0; i--)
            {
                var rule = _rules[i];

                try
                {
                    if (rule.IsMatch(request))
                    {
                        matchingRules.Add(rule);
                        invokeEvent |= rule.InvokeRequestEvents;

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

            if (invokeEvent)
            {
                InvokeRequestProcessed(request);
            }

            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var invokeEvent = InvokeResponseEventsAlways;

            if (response.RequestMessage != null && _requestMatches.TryGetValue(response.RequestMessage, out var match))
            {
                foreach (var rule in match.MatchingRules)
                {
                    invokeEvent |= rule.InvokeResponseEvents;
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
                _requestMatches.TryRemove(response.RequestMessage, out _);
            }

            if (invokeEvent)
            {
                InvokeResponseReceived(response);
            }

            return response;
        }

        private void InvokeRequestProcessed(HttpRequestMessage request)
        {
            try
            {
                RequestProcessed?.Invoke(this, request);
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

        private void InvokeResponseReceived(HttpResponseMessage response)
        {
            try
            {
                ResponseReceived?.Invoke(this, response);
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
