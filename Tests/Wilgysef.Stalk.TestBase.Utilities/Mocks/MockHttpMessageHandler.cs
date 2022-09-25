using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Wilgysef.Stalk.TestBase.Utilities.Mocks
{
    using AsyncEndpointAction = Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>;
    using EndpointAction = Func<HttpRequestMessage, HttpResponseMessage>;

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public AsyncEndpointAction DefaultEndpointAction { get; set; } = DefaultEndpointActionFunc;

        public HttpRequestMessageLog Requests => _requests;
        private readonly HttpRequestMessageLog _requests = new HttpRequestMessageLog();

        private readonly List<EndpointActionEntry> _actions = new List<EndpointActionEntry>();

        public MockHttpMessageHandler AddEndpoint(string uri, AsyncEndpointAction action)
        {
            _actions.Add(new EndpointActionEntry(uri, action));
            return this;
        }

        public MockHttpMessageHandler AddEndpoint(Uri uri, AsyncEndpointAction action)
        {
            _actions.Add(new EndpointActionEntry(uri, action));
            return this;
        }

        public MockHttpMessageHandler AddEndpoint(Regex uri, AsyncEndpointAction action)
        {
            _actions.Add(new EndpointActionEntry(uri, action));
            return this;
        }

        public MockHttpMessageHandler AddEndpoint(string uri, EndpointAction action)
        {
            _actions.Add(new EndpointActionEntry(uri, action));
            return this;
        }

        public MockHttpMessageHandler AddEndpoint(Uri uri, EndpointAction action)
        {
            _actions.Add(new EndpointActionEntry(uri, action));
            return this;
        }

        public MockHttpMessageHandler AddEndpoint(Regex uri, EndpointAction action)
        {
            _actions.Add(new EndpointActionEntry(uri, action));
            return this;
        }

        public MockHttpMessageHandler AddEndpointFromManifestResource(string uri, string name, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return AddEndpoint(uri, request =>
            {
                var response = ResponseFromManifestResource(name);
                response.StatusCode = statusCode;
                return response;
            });
        }

        public MockHttpMessageHandler AddEndpointFromManifestResource(Uri uri, string name, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return AddEndpoint(uri, request =>
            {
                var response = ResponseFromManifestResource(name);
                response.StatusCode = statusCode;
                return response;
            });
        }

        public MockHttpMessageHandler AddEndpointFromManifestResource(Regex uri, string name, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return AddEndpoint(uri, request =>
            {
                var response = ResponseFromManifestResource(name);
                response.StatusCode = statusCode;
                return response;
            });
        }

        public static Task<HttpResponseMessage> DefaultEndpointActionFunc(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.AddEntry(request);

            HttpResponseMessage? response = null;

            foreach (var action in _actions)
            {
                if ((action.RequestString != null && action.RequestString == request.RequestUri.AbsoluteUri)
                    || (action.RequestUri != null && action.RequestUri == request.RequestUri)
                    || (action.RequestRegex != null && action.RequestRegex.IsMatch(request.RequestUri.AbsoluteUri)))
                {
                    response = action.AsyncEndpointAction != null
                        ? await action.AsyncEndpointAction(request, cancellationToken)
                        : action.EndpointAction!(request);
                    break;
                }
            }

            return response
                ?? await DefaultEndpointAction(request, cancellationToken);
        }

        private HttpResponseMessage ResponseFromManifestResource(string name)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            return new HttpResponseMessage
            {
                Content = new StreamContent(stream),
            };
        }

        private class EndpointActionEntry
        {
            public string? RequestString { get; }

            public Uri? RequestUri { get; }

            public Regex? RequestRegex { get; }

            public AsyncEndpointAction? AsyncEndpointAction { get; }

            public EndpointAction? EndpointAction { get; }

            public EndpointActionEntry(string request, AsyncEndpointAction action)
            {
                RequestString = request;
                AsyncEndpointAction = action;
            }

            public EndpointActionEntry(Uri request, AsyncEndpointAction action)
            {
                RequestUri = request;
                AsyncEndpointAction = action;
            }

            public EndpointActionEntry(Regex request, AsyncEndpointAction action)
            {
                RequestRegex = request;
                AsyncEndpointAction = action;
            }

            public EndpointActionEntry(string request, EndpointAction action)
            {
                RequestString = request;
                EndpointAction = action;
            }

            public EndpointActionEntry(Uri request, EndpointAction action)
            {
                RequestUri = request;
                EndpointAction = action;
            }

            public EndpointActionEntry(Regex request, EndpointAction action)
            {
                RequestRegex = request;
                EndpointAction = action;
            }
        }
    }

    public class HttpRequestMessageLog : ICollection<HttpRequestMessageEntry>
    {
        public int Count => _requestEntries.Count;

        public bool IsReadOnly => true;

        private readonly List<HttpRequestMessageEntry> _requestEntries = new List<HttpRequestMessageEntry>();

        internal void AddEntry(HttpRequestMessage request)
        {
            _requestEntries.Add(new HttpRequestMessageEntry(request, DateTime.Now));
        }

        public void Add(HttpRequestMessageEntry item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(HttpRequestMessageEntry item)
        {
            return _requestEntries.Contains(item);
        }

        public void CopyTo(HttpRequestMessageEntry[] array, int arrayIndex)
        {
            _requestEntries.CopyTo(array, arrayIndex);
        }

        public bool Remove(HttpRequestMessageEntry item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<HttpRequestMessageEntry> GetEnumerator()
        {
            return _requestEntries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _requestEntries.GetEnumerator();
        }
    }

    public class HttpRequestMessageEntry
    {
        public HttpRequestMessage Request { get; }

        public DateTime Sent { get; }

        public HttpRequestMessageEntry(HttpRequestMessage request, DateTime sent)
        {
            Request = request;
            Sent = sent;
        }
    }
}
