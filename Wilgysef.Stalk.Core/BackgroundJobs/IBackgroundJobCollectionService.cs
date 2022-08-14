using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobCollectionService : ISingletonDependency
{
    /// <summary>
    /// Active background jobs.
    /// </summary>
    IReadOnlyCollection<BackgroundJob> ActiveJobs { get; }

    void AddActiveJob(BackgroundJob job);

    void RemoveActiveJob(BackgroundJob job);
}
