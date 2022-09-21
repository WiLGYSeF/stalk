using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Wilgysef.Stalk.Core.JobHttpClientCollectionServices;

public class JobHttpClientCollectionService : IJobHttpClientCollectionService
{
    private readonly ConcurrentDictionary<long, HttpClient> _clients = new();

    public void AddHttpClient(long jobId, HttpClient client)
    {
        if (!_clients.TryAdd(jobId, client))
        {
            throw new ArgumentException("Job Id already exists in collection.", nameof(jobId));
        }
    }

    public HttpClient GetHttpClient(long jobId)
    {
        return _clients[jobId];
    }

    public bool SetHttpClient(long jobId, HttpClient client)
    {
        if (TryGetHttpClient(jobId, out var oldClient))
        {
            oldClient.Dispose();
            _clients[jobId] = client;
            return true;
        }

        AddHttpClient(jobId, client);
        return false;
    }

    public bool TryGetHttpClient(long jobId, [MaybeNullWhen(false)] out HttpClient client)
    {
        return _clients.TryGetValue(jobId, out client);
    }

    public bool RemoveHttpClient(long jobId)
    {
        return _clients.Remove(jobId, out _);
    }
}
