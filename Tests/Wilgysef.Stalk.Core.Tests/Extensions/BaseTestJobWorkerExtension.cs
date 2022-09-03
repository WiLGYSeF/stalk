using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.Extensions;

internal static class BaseTestJobWorkerExtension
{
    public static async Task<Job> WaitUntilJobAsync(this BaseTest baseTest,
        long jobId,
        Func<Job, bool> condition,
        TimeSpan timeout,
        TimeSpan? interval = null)
    {
        Job job = null!;
        await BaseTest.WaitUntilAsync(async () =>
        {
            job = await ReloadJobAsync(baseTest, jobId);
            return condition(job);
        }, timeout, interval ?? TimeSpan.Zero);
        return job;
    }

    public static async Task<Job> WaitUntilJobAsync(this BaseTest baseTest,
        long jobId,
        Func<Job, Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? interval = null)
    {
        Job job = null!;
        await BaseTest.WaitUntilAsync(async () =>
        {
            job = await ReloadJobAsync(baseTest, jobId);
            return await condition(job);
        }, timeout, interval ?? TimeSpan.Zero);
        return job;
    }

    public static async Task<Job> ReloadJobAsync(this BaseTest baseTest, long jobId)
    {
        using var scope = baseTest.BeginLifetimeScope();
        return await ReloadJobAsync(baseTest, jobId, scope);
    }

    public static async Task<Job> ReloadJobAsync(this BaseTest _, long jobId, IServiceLifetimeScope scope)
    {
        var jobManager = scope.GetRequiredService<IJobManager>();
        return await jobManager.GetJobAsync(jobId);
    }
}
