using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobScopeServices;

public interface IJobScopeCollectionService
{
    /// <summary>
    /// Gets or creates a job lifetime scope.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <param name="serviceLocator">Service locator.</param>
    /// <returns>Job lifetime scope.</returns>
    IServiceLifetimeScope GetJobScope(long jobId, IServiceLocator serviceLocator);

    /// <summary>
    /// Removes a job lifetime scope.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see langword="true"/> if the job scope was removed, otherwise <see langword="false"/>.</returns>
    bool RemoveJobScope(long jobId);
}
