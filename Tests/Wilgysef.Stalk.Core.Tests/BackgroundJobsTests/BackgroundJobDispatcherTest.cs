using Shouldly;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.BackgroundJobsTests;

public class BackgroundJobDispatcherTest : BaseTest
{
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;
    private readonly IBackgroundJobManager _backgroundJobManager;

    public BackgroundJobDispatcherTest()
    {
        ReplaceService<TestJobHandler, IBackgroundJobHandler<TestJobArgs>>();
        ReplaceService<TestJobFailHandler, IBackgroundJobHandler<TestJobFailArgs>>();

        _backgroundJobDispatcher = GetRequiredService<IBackgroundJobDispatcher>();
        _backgroundJobManager = GetRequiredService<IBackgroundJobManager>();
    }

    [Fact]
    public async Task Executes_Jobs()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestJobArgs("a", 1),
                argsName: typeof(TestJobArgs).AssemblyQualifiedName),
            true);
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                2,
                new TestJobArgs("b", 2),
                argsName: typeof(TestJobArgs).AssemblyQualifiedName),
            true);

        var jobs = await _backgroundJobManager.GetJobsAsync();
        jobs.Count.ShouldBe(2);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        jobs = await _backgroundJobManager.GetJobsAsync();
        jobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Executes_Failed_Job()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestJobFailArgs(),
                argsName: typeof(TestJobFailArgs).AssemblyQualifiedName),
            true);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        await WithLifetimeScopeAsync(async scope =>
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.Attempts.ShouldBe(1);
            job.Abandoned.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Executes_Invalid_Job()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestJobFailArgs(),
                argsName: "invalid"),
            true);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        await WithLifetimeScopeAsync(async scope =>
        {
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            var job = await backgroundJobManager.FindJobAsync(1);
            job!.Attempts.ShouldBe(0);
            job.Abandoned.ShouldBeTrue();
        });
    }

    private class TestJobHandler : IBackgroundJobHandler<TestJobArgs>
    {
        public Task ExecuteJobAsync(TestJobArgs args, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class TestJobFailHandler : IBackgroundJobHandler<TestJobFailArgs>
    {
        public Task ExecuteJobAsync(TestJobFailArgs args, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException();
        }
    }

    private class TestJobArgs : BackgroundJobArgs
    {
        public string Name { get; }

        public int Value { get; }

        public TestJobArgs(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    private class TestJobFailArgs : BackgroundJobArgs { }
}
