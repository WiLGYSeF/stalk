using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public interface IExtractorHttpClientCollectionService
{
    /// <summary>
    /// Gets an <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <param name="extractor">Extractor.</param>
    /// <param name="extractorConfig">Extractor config, used if no existing HTTP client exists.</param>
    /// <returns><see cref="HttpClient"/>.</returns>
    HttpClient GetHttpClient(long jobId, IExtractor extractor, IDictionary<string, object?> extractorConfig);

    /// <summary>
    /// Removes <see cref="HttpClient"/>s from a job.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see langword="true"/> if clients were removed, otherwise <see langword="false"/>.</returns>
    bool RemoveHttpClients(long jobId);
}
