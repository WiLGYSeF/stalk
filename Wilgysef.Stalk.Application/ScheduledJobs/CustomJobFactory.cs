using Quartz;
using Quartz.Spi;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application.ScheduledJobs;

internal class CustomJobFactory : IJobFactory, ISingletonDependency
{
    private readonly IServiceLifetimeScope _scope;

    public CustomJobFactory(
        IServiceLocator serviceLocator)
    {
        _scope = serviceLocator.BeginLifetimeScopeFromRoot();
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return (_scope.BeginLifetimeScope().GetRequiredService(bundle.JobDetail.JobType) as IJob)!;
    }

    public void ReturnJob(IJob job)
    {
        (job as IDisposable)?.Dispose();
    }
}
