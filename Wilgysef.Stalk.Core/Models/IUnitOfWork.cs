namespace Wilgysef.Stalk.Core.Models;

public interface IUnitOfWork
{
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
