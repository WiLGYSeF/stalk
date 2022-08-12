using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobHandler<T> : ITransientDependency where T : BackgroundJobArgs
{
    Task ExecuteJobAsync(T args);
}
