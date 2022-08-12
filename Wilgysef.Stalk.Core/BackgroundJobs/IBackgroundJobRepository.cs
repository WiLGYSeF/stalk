using Wilgysef.Stalk.Core.Models;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobRepository : IRepository<BackgroundJob>, ISpecificationRepository<BackgroundJob>
{
}
