using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Wilgysef.HttpClientInterception
{
    public class HttpClientInterceptionRuleBuilder
    {
        public IList<Func<HttpRequestMessage, bool>> RequestFilters = new List<Func<HttpRequestMessage, bool>>();

        public Func<HttpRequestMessage, HttpResponseMessage>? SendResponseMessage
        {
            get => _sendResponseMessage;
            set
            {
                _sendResponseMessage = value;
                if (_sendResponseMessage != null)
                {
                    SendResponseMessageAsync = null;
                    UnsetResponseMessage();
                }
            }
        }
        private Func<HttpRequestMessage, HttpResponseMessage>? _sendResponseMessage;

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? SendResponseMessageAsync
        {
            get => _sendResponseMessageAsync;
            set
            {
                _sendResponseMessageAsync = value;
                if (_sendResponseMessageAsync != null)
                {
                    SendResponseMessage = null;
                    UnsetResponseMessage();
                }
            }
        }
        private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _sendResponseMessageAsync;

        public Func<HttpRequestMessage, HttpRequestMessage>? ModifyRequest { get; set; }

        public Func<HttpResponseMessage, HttpResponseMessage>? ModifyResponse { get; set; }

        public bool LogRequest { get; set; }

        public bool LogResponse { get; set; }

        public HttpStatusCode? ResponseCode
        {
            get => _responseCode;
            set
            {
                _responseCode = value;
                if (_responseCode != null)
                {
                    UnsetSendMessageResponse();
                }
            }
        }
        private HttpStatusCode? _responseCode;

        public HttpContent? ResponseContent
        {
            get => _responseContent;
            set
            {
                _responseContent = value;
                if (_responseContent != null)
                {
                    UnsetSendMessageResponse();
                }
            }
        }
        private HttpContent? _responseContent;

        public IDictionary<string, IEnumerable<string>> ResponseHeaders => _responseHeaders;

        private Func<HttpRequestMessage, bool>? _versionFilter;
        private Func<HttpRequestMessage, bool>? _methodFilter;
        private Func<HttpRequestMessage, bool>? _uriFilter;
        private Func<HttpRequestMessage, bool>? _schemeFilter;
        private Func<HttpRequestMessage, bool>? _hostFilter;
        private Func<HttpRequestMessage, bool>? _portFilter;
        private Func<HttpRequestMessage, bool>? _pathFilter;
        private Func<HttpRequestMessage, bool>? _fragmentFilter;

        private readonly List<Func<HttpRequestMessage, bool>> _queryFilters = new List<Func<HttpRequestMessage, bool>>();
        private bool _queryWithParameter;

        private readonly Dictionary<string, IEnumerable<string>> _responseHeaders = new Dictionary<string, IEnumerable<string>>();

        public HttpClientInterceptionRule Create()
        {
            var rule = new HttpClientInterceptionRule
            {
                ModifyRequest = ModifyRequest,
                ModifyResponse = ModifyResponse,
                LogRequest = LogRequest,
                LogResponse = LogResponse,
            };

            if (SendResponseMessage != null || SendResponseMessageAsync != null)
            {
                rule.SendResponseMessage = SendResponseMessage;
                rule.SendResponseMessageAsync = SendResponseMessageAsync;
            }
            else if (ResponseCode != null || ResponseContent != null)
            {
                rule.SendResponseMessage = request =>
                {
                    var response = new HttpResponseMessage(ResponseCode ?? HttpStatusCode.OK);
                    if (ResponseContent != null)
                    {
                        response.Content = ResponseContent;
                    }
                    
                    foreach (var (header, value) in ResponseHeaders)
                    {
                        response.Headers.Add(header, value);
                    }

                    return response;
                };
            }
            else if (ResponseHeaders.Count > 0)
            {
                throw new InvalidOperationException("Response headers need to be accompanied with either a status code or response content.");
            }

            var filterFuncs = new[]
            {
                _versionFilter,
                _methodFilter,
                _uriFilter,
                _schemeFilter,
                _hostFilter,
                _portFilter,
                _pathFilter,
                _fragmentFilter,
            };

            foreach (var filter in filterFuncs)
            {
                if (filter != null)
                {
                    rule.RequestFilters.Add(filter);
                }
            }

            var filterFuncLists = new[]
            {
                _queryFilters,
                RequestFilters,
            };

            foreach (var filterList in filterFuncLists)
            {
                foreach (var filter in filterList)
                {
                    rule.RequestFilters.Add(filter);
                }
            }

            return rule;
        }

        public HttpClientInterceptionRuleBuilder ForVersion(Version version)
        {
            _versionFilter = request => request.Version == version;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForVersions(params Version[] versions)
        {
            _versionFilter = request => versions.Contains(request.Version);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForVersions(IEnumerable<Version> versions)
        {
            _versionFilter = request => versions.Contains(request.Version);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForVersionRange(Version minVersion, Version maxVersion)
        {
            _versionFilter = request => minVersion <= request.Version && request.Version <= maxVersion;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForMethod(HttpMethod method)
        {
            _methodFilter = request => request.Method == method;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForMethods(params HttpMethod[] methods)
        {
            _methodFilter = request => methods.Contains(request.Method);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForMethods(IEnumerable<HttpMethod> methods)
        {
            _methodFilter = request => methods.Contains(request.Method);
            return this;
        }

        #region Uri

        public HttpClientInterceptionRuleBuilder ForUri(Uri uri)
        {
            _uriFilter = request => request.RequestUri == uri;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForUri(string uri)
        {
            _uriFilter = request =>  request.RequestUri.OriginalString == uri || request.RequestUri.AbsoluteUri == uri;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForUri(Regex regex)
        {
            _uriFilter = request => regex.IsMatch(request.RequestUri.AbsoluteUri);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForUriStartsWith(string uri)
        {
            _uriFilter = request => request.RequestUri.AbsoluteUri.StartsWith(uri);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForScheme(string scheme)
        {
            _schemeFilter = request => request.RequestUri.Scheme == scheme;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForScheme(Regex regex)
        {
            _schemeFilter = request => regex.IsMatch(request.RequestUri.Scheme);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForSchemes(params string[] schemes)
        {
            _schemeFilter = request => schemes.Contains(request.RequestUri.Scheme);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForSchemes(IEnumerable<string> schemes)
        {
            _schemeFilter = request => schemes.Contains(request.RequestUri.Scheme);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForHost(string host)
        {
            _hostFilter = request => request.RequestUri.Host == host;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForHost(Regex regex)
        {
            _hostFilter = request => regex.IsMatch(request.RequestUri.Host);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForPort(int port)
        {
            _portFilter = request => request.RequestUri.Port == port;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForPorts(params int[] ports)
        {
            _portFilter = request => ports.Contains(request.RequestUri.Port);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForPorts(IEnumerable<int> ports)
        {
            _portFilter = request => ports.Contains(request.RequestUri.Port);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForPortRange(int minPort, int maxPort)
        {
            _portFilter = request => minPort <= request.RequestUri.Port && request.RequestUri.Port <= maxPort;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForPath(string path)
        {
            _pathFilter = request => request.RequestUri.AbsolutePath == path;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForPath(Regex regex)
        {
            _pathFilter = request => regex.IsMatch(request.RequestUri.AbsolutePath);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForPathStartsWith(string path)
        {
            _pathFilter = request => request.RequestUri.AbsolutePath.StartsWith(path);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForQuery(string query)
        {
            _queryFilters.Clear();
            _queryFilters.Add(request => request.RequestUri.Query == query);
            _queryWithParameter = false;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForQuery(Regex regex)
        {
            _queryFilters.Clear();
            _queryFilters.Add(request => regex.IsMatch(request.RequestUri.Query));
            _queryWithParameter = false;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForQueryWithParameter(string parameter)
        {
            if (!_queryWithParameter)
            {
                _queryFilters.Clear();
            }

            _queryFilters.Add(request =>
            {
                var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
                return query[parameter] != null;
            });
            _queryWithParameter = true;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForQueryWithParameter(string parameter, string value)
        {
            if (!_queryWithParameter)
            {
                _queryFilters.Clear();
            }

            _queryFilters.Add(request =>
            {
                var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
                return query[parameter] == value;
            });
            _queryWithParameter = true;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForQueryWithParameter(string parameter, Func<string?, bool> valuePredicate)
        {
            if (!_queryWithParameter)
            {
                _queryFilters.Clear();
            }

            _queryFilters.Add(request =>
            {
                var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
                return valuePredicate(query[parameter]);
            });
            _queryWithParameter = true;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForFragment(string fragment)
        {
            _fragmentFilter = request => request.RequestUri.Fragment == fragment;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForFragment(Regex regex)
        {
            _fragmentFilter = request => regex.IsMatch(request.RequestUri.Fragment);
            return this;
        }

        #endregion

        public HttpClientInterceptionRuleBuilder RespondWith(Func<HttpRequestMessage, HttpResponseMessage>? responseMessage)
        {
            SendResponseMessage = responseMessage;
            return this;
        }

        public HttpClientInterceptionRuleBuilder RespondWith(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? responseMessage)
        {
            SendResponseMessageAsync = responseMessage;
            return this;
        }

        public HttpClientInterceptionRuleBuilder RespondWithStatus(HttpStatusCode statusCode)
        {
            ResponseCode = statusCode;
            return this;
        }

        public HttpClientInterceptionRuleBuilder RespondWithContent(string content)
        {
            return RespondWithContent(new StringContent(content));
        }

        public HttpClientInterceptionRuleBuilder RespondWithContent(byte[] content)
        {
            return RespondWithContent(new ByteArrayContent(content));
        }

        public HttpClientInterceptionRuleBuilder RespondWithContent(Stream stream)
        {
            return RespondWithContent(new StreamContent(stream));
        }

        public HttpClientInterceptionRuleBuilder RespondWithContent(HttpContent content)
        {
            ResponseContent = content;
            return this;
        }

        public HttpClientInterceptionRuleBuilder RespondWithHeader(string name, string value)
        {
            ResponseHeaders[name] = new[] { value };
            return this;
        }

        public HttpClientInterceptionRuleBuilder RespondWithHeader(string name, IEnumerable<string> values)
        {
            ResponseHeaders[name] = values;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ForRequest(Func<HttpRequestMessage, bool> predicate)
        {
            RequestFilters.Add(predicate);
            return this;
        }

        public HttpClientInterceptionRuleBuilder ModifyRequestWith(Func<HttpRequestMessage, HttpRequestMessage>? modifyRequest)
        {
            ModifyRequest = modifyRequest;
            return this;
        }

        public HttpClientInterceptionRuleBuilder ModifyResponseWith(Func<HttpResponseMessage, HttpResponseMessage>? modifyResponse)
        {
            ModifyResponse = modifyResponse;
            return this;
        }

        public HttpClientInterceptionRuleBuilder LogRequestMessage(bool log = true)
        {
            LogRequest = log;
            return this;
        }

        public HttpClientInterceptionRuleBuilder LogResponseMessage(bool log = true)
        {
            LogResponse = log;
            return this;
        }

        private void UnsetSendMessageResponse()
        {
            SendResponseMessage = null;
            SendResponseMessageAsync = null;
        }

        private void UnsetResponseMessage()
        {
            ResponseCode = null;
            ResponseContent = null;
            ResponseHeaders.Clear();
        }
    }
}
