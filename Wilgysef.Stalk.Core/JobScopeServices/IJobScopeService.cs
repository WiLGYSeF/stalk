﻿using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobScopeServices;

public interface IJobScopeService
{
    /// <summary>
    /// Gets or creates a job lifetime scope.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns>Job lifetime scope.</returns>
    IServiceLifetimeScope GetJobScope(long jobId);

    /// <summary>
    /// Adds job logger.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <param name="logger">Job logger.</param>
    void AddJobLogger(long jobId, ILogger? logger);

    /// <summary>
    /// Gets job logger.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns>Job logger.</returns>
    ILogger? GetJobLogger(long jobId);

    /// <summary>
    /// Removes a job lifetime scope.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see langword="true"/> if the job scope was removed, otherwise <see langword="false"/>.</returns>
    bool RemoveJobScope(long jobId);
}
