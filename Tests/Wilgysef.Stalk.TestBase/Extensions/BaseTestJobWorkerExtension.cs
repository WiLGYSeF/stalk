using Shouldly;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Extensions;

public static class BaseTestJobWorkerExtension
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    public static async Task<Job> WaitUntilJobAsync(this BaseTest baseTest,
        long jobId,
        Func<Job, bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null,
        bool shouldCheckCondition = true)
    {
        Job job = null!;
        await BaseTest.WaitUntilAsync(async () =>
        {
            job = await ReloadJobAsync(baseTest, jobId);
            return condition(job);
        }, timeout ?? DefaultTimeout, interval ?? TimeSpan.Zero);

        if (shouldCheckCondition)
        {
            condition(job).ShouldBeTrue();
        }

        return job;
    }

    public static async Task<Job> WaitUntilJobAsync(this BaseTest baseTest,
        long jobId,
        Func<Job, Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null,
        bool shouldCheckCondition = true)
    {
        Job job = null!;
        await BaseTest.WaitUntilAsync(async () =>
        {
            job = await ReloadJobAsync(baseTest, jobId);
            return await condition(job);
        }, timeout ?? DefaultTimeout, interval ?? TimeSpan.Zero);

        if (shouldCheckCondition)
        {
            (await condition(job)).ShouldBeTrue();
        }

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
