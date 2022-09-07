using System.Diagnostics.CodeAnalysis;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobHttpClientCollectionServices;

public interface IJobHttpClientCollectionService : ISingletonDependency
{
    /// <summary>
    /// Adds an <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <param name="client"><see cref="HttpClient"/>.</param>
    /// <exception cref="ArgumentException">Job Id already exists in collection.</exception>
    void AddHttpClient(long jobId, HttpClient client);

    /// <summary>
    /// Gets an <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see cref="HttpClient"/>.</returns>
    HttpClient GetHttpClient(long jobId);

    /// <summary>
    /// Sets the <see cref="HttpClient"/>, disposing the old client.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <param name="client"><see cref="HttpClient"/>.</param>
    /// <returns><see langword="true"/> if the old client value was replaced and disposed, otherwise <see langword="false"/>.</returns>
    bool SetHttpClient(long jobId, HttpClient client);

    /// <summary>
    /// Tries to get an <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <param name="client"><see cref="HttpClient"/>.</param>
    /// <returns><see langword="true"/> if a client was found, otherwise <see langword="false"/>.</returns>
    bool TryGetHttpClient(long jobId, [MaybeNullWhen(false)] out HttpClient client);

    /// <summary>
    /// Removes an <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see langword="true"/> if a client was removed, otherwise <see langword="false"/>.</returns>
    bool RemoveHttpClient(long jobId);
}
