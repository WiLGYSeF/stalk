using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobDispatcher : ITransientDependency
{
    Task DispatchJobs<T>(params T[] args) where T : notnull, BackgroundJobArgs;
}
