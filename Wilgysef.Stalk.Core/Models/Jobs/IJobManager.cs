﻿using Ardalis.Specification;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobManager : ITransientDependency
{
    /// <summary>
    /// Creates a job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Job> CreateJobAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job by its Id.
    /// </summary>
    /// <param name="id">Job Id.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Job.</returns>
    Task<Job> GetJobAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job by a job task Id.
    /// </summary>
    /// <param name="id">Job task Id.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Job.</returns>
    Task<Job> GetJobByTaskIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of jobs.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>List of jobs.</returns>
    Task<List<Job>> GetJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of jobs by specification.
    /// </summary>
    /// <param name="specification">Specification.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>List of jobs.</returns>
    Task<List<Job>> GetJobsAsync(ISpecification<Job> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets next priority queued job.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Job, or <see langword="null"/> if there are no queued jobs.</returns>
    Task<Job?> GetNextPriorityJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Job> UpdateJobAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteJobAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets job as active.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SetJobActiveAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets job as done.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SetJobDoneAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets all active and transitioning jobs and tasks to their inactive and transitioned states.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeactivateJobsAsync(CancellationToken cancellationToken = default);
}
