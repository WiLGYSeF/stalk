using Microsoft.Extensions.Logging;
using Shouldly;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.BackgroundJobsTests;

public class BackgroundJobDispatcherTest : BaseTest
{
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;
    private readonly IBackgroundJobManager _backgroundJobManager;

    public BackgroundJobDispatcherTest()
    {
        ReplaceService<TestJobHandler, IBackgroundJobHandler<TestJobArgs>>();
        ReplaceService<TestFailJobHandler, IBackgroundJobHandler<TestFailJobArgs>>();
        ReplaceService<TestChangeJobHandler, IBackgroundJobHandler<TestChangeJobArgs>>();

        _backgroundJobDispatcher = GetRequiredService<IBackgroundJobDispatcher>();
        _backgroundJobManager = GetRequiredService<IBackgroundJobManager>();
    }

    [Fact]
    public async Task Executes_Jobs()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestJobArgs(),
                argsName: typeof(TestJobArgs).AssemblyQualifiedName),
            true);
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                2,
                new TestJobArgs(),
                argsName: typeof(TestJobArgs).AssemblyQualifiedName),
            true);

        var jobs = await _backgroundJobManager.GetJobsAsync();
        jobs.Count.ShouldBe(2);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        jobs = await _backgroundJobManager.GetJobsAsync();
        jobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Executes_Job_Change_JobEntity()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestChangeJobArgs(),
                argsName: typeof(TestChangeJobArgs).AssemblyQualifiedName),
            true);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        using (var scope = BeginLifetimeScope())
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.Attempts.ShouldBe(1);
            job.Abandoned.ShouldBeFalse();

            job.NextRun.ShouldNotBeNull();
            (DateTime.Now.AddSeconds(123) - job.NextRun.Value).Duration().TotalSeconds.ShouldBeInRange(0, 3);
        }
    }

    [Fact]
    public async Task Abandons_Expired_Jobs()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestJobArgs(),
                maximumLifetime: DateTime.Now.AddDays(-1),
                argsName: typeof(TestChangeJobArgs).AssemblyQualifiedName),
            true);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        using (var scope = BeginLifetimeScope())
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.Attempts.ShouldBe(0);
            job.Abandoned.ShouldBeTrue();
        };
    }

    [Fact]
    public async Task Abandons_MaxAttempts_Jobs()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestJobArgs(),
                maximumAttempts: 1,
                argsName: typeof(TestChangeJobArgs).AssemblyQualifiedName),
            true);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        using (var scope = BeginLifetimeScope())
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.Attempts.ShouldBe(1);
            job.Abandoned.ShouldBeTrue();
        };
    }

    [Fact]
    public async Task Executes_Failed_Job()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestFailJobArgs(),
                argsName: typeof(TestFailJobArgs).AssemblyQualifiedName),
            true);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        using (var scope = BeginLifetimeScope())
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.Attempts.ShouldBe(1);
            job.Abandoned.ShouldBeFalse();
        };
    }

    [Fact]
    public async Task Executes_Retries_Job()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestFailJobArgs(),
                argsName: typeof(TestFailJobArgs).AssemblyQualifiedName),
            true);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        using (var scope = BeginLifetimeScope())
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.ChangeNextRun(null);
            await backgroundJobManager.UpdateJobAsync(job);
        }

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        using (var scope = BeginLifetimeScope())
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.Attempts.ShouldBe(2);
            job.Abandoned.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Executes_Invalid_Job()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestFailJobArgs(),
                argsName: "invalid"),
            true);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        using (var scope = BeginLifetimeScope())
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.Attempts.ShouldBe(0);
            job.Abandoned.ShouldBeTrue();
        }
    }

    private class TestJobHandler : BackgroundJobHandler<TestJobArgs>
    {
        public override Task ExecuteJobAsync(TestJobArgs args, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class TestFailJobHandler : BackgroundJobHandler<TestFailJobArgs>
    {
        public override Task ExecuteJobAsync(TestFailJobArgs args, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException();
        }
    }

    private class TestChangeJobHandler : BackgroundJobHandler<TestChangeJobArgs>
    {
        public override Task ExecuteJobAsync(TestChangeJobArgs args, CancellationToken cancellationToken = default)
        {
            BackgroundJob.GetNextRunOffset = () => TimeSpan.FromSeconds(123);
            throw new InvalidOperationException();
        }
    }

    private class TestJobArgs : BackgroundJobArgs { }

    private class TestFailJobArgs : BackgroundJobArgs { }

    private class TestChangeJobArgs : BackgroundJobArgs { }
}
