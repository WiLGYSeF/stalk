using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models;

public interface IUnitOfWork : IScopedDependency
{
    /// <summary>
    /// Job repository.
    /// </summary>
    IJobRepository JobRepository { get; }

    /// <summary>
    /// Job task repository.
    /// </summary>
    IJobTaskRepository JobTaskRepository { get; }

    /// <summary>
    /// Background job repository.
    /// </summary>
    IBackgroundJobRepository BackgroundJobRepository { get; }

    /// <summary>
    /// Save changes.
    /// </summary>
    /// <returns>Number of state entries written to the database.</returns>
    int SaveChanges();

    /// <summary>
    /// Save changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
