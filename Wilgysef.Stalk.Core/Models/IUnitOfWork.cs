using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models;

public interface IUnitOfWork : IScopedDependency
{
    IJobRepository JobRepository { get; }

    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
