using Shouldly;
using System.Reflection;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.BackgroundJobsTests;

public class BackgroundJobManagerTest : BaseTest
{
    private readonly IBackgroundJobManager _backgroundJobManager;

    public BackgroundJobManagerTest()
    {
        _backgroundJobManager = GetRequiredService<IBackgroundJobManager>();
    }

    [Fact]
    public async Task Enqueue_Job()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestJobArgs("a", 1),
                argsName: typeof(TestJobArgs).AssemblyQualifiedName),
            true);

        var job = await _backgroundJobManager.FindJobAsync(1);
        job.ShouldNotBeNull();
    }

    [Fact]
    public async Task Enqueue_Replace_Job()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new TestJobArgs("a", 1),
                argsName: typeof(TestJobArgs).AssemblyQualifiedName),
            true);
        await _backgroundJobManager.EnqueueOrReplaceJobAsync(
            BackgroundJob.Create(
                2,
                new TestJobArgs("b", 2),
                argsName: typeof(TestJobArgs).AssemblyQualifiedName),
            true);

        var jobs = await _backgroundJobManager.GetJobsAsync();
        var job = jobs.Single(j => j.GetJobArgsType() == typeof(TestJobArgs));
        var args = job.DeserializeArgs() as TestJobArgs;

        args.Name.ShouldBe("b");
        args.Value.ShouldBe(2);
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
}
