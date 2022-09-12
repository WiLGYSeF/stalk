using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace Wilgysef.Stalk.Extractors.Twitter.Tests
{
    using EndpointAction = Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>;

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public EndpointAction DefaultEndpointAction { get; set; } = DefaultEndpointActionFunc;

        private readonly Dictionary<Uri, EndpointAction> _uriEndpoints = new Dictionary<Uri, EndpointAction>();
        private readonly Dictionary<Regex, EndpointAction> _regexEndpoints = new Dictionary<Regex, EndpointAction>();

        public MockHttpMessageHandler AddEndpoint(Uri uri, EndpointAction action)
        {
            _uriEndpoints[uri] = action;
            return this;
        }

        public MockHttpMessageHandler AddEndpoint(Regex regex, EndpointAction action)
        {
            _regexEndpoints[regex] = action;
            return this;
        }

        public MockHttpMessageHandler AddEndpointFromManifestResource(Uri uri, string name, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _uriEndpoints[uri] = (HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                var response = ResponseFromManifestResource(name);
                response.StatusCode = statusCode;
                return Task.FromResult(response);
            };
            return this;
        }

        public MockHttpMessageHandler AddEndpointFromManifestResource(Regex regex, string name, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _regexEndpoints[regex] = (HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                var response = ResponseFromManifestResource(name);
                response.StatusCode = statusCode;
                return Task.FromResult(response);
            };
            return this;
        }

        public static Task<HttpResponseMessage> DefaultEndpointActionFunc(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            foreach (var pair in _uriEndpoints)
            {
                if (pair.Key == request.RequestUri)
                {
                    return pair.Value(request, cancellationToken);
                }
            }

            foreach (var pair in _regexEndpoints)
            {
                if (pair.Key.IsMatch(request.RequestUri.AbsoluteUri))
                {
                    return pair.Value(request, cancellationToken);
                }
            }

            return DefaultEndpointAction(request, cancellationToken);
        }

        private HttpResponseMessage ResponseFromManifestResource(string name)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            return new HttpResponseMessage
            {
                Content = new StreamContent(stream),
            };
        }
    }
}
