namespace Wilgysef.Stalk.TestBase.Mocks;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _callback;

    private readonly HttpRequestMessageLog _requestLog;

    public MockHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback,
        HttpRequestMessageLog requestLog)
    {
        _callback = callback;
        _requestLog = requestLog;
    }

    public MockHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> callback,
        HttpRequestMessageLog requestLog)
    {
        _callback = CallbackWrapper;
        _requestLog = requestLog;

        Task<HttpResponseMessage> CallbackWrapper(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(callback(request, cancellationToken));
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requestLog.AddEntry(request);
        return await _callback(request, cancellationToken);
    }
}

public class HttpRequestMessageLog
{
    public IReadOnlyList<HttpRequestMessageEntry> RequestEntries => _requestEntries;

    private readonly List<HttpRequestMessageEntry> _requestEntries = new();

    public void AddEntry(HttpRequestMessage request)
    {
        _requestEntries.Add(new HttpRequestMessageEntry(request, DateTime.Now));
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
