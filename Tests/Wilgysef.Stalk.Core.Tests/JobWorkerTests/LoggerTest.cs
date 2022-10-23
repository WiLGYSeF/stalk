using Autofac;
using Microsoft.Extensions.Logging;
using Shouldly;
using Wilgysef.Stalk.Core.JobScopeServices;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Loggers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Core.Tests.JobWorkerTests;

public class LoggerTest : BaseTest
{
    private readonly IJobManager _jobManager;

    public LoggerTest()
    {
        ReplaceSingletonService<IJobTaskWorkerFactory>(c => new JobTaskWorkerFactoryMock(
            c.Resolve<IServiceLocator>()));

        _jobManager = GetRequiredService<IJobManager>();
    }

    [Fact]
    public async Task Config_Logging()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithConfig(new JobConfig
            {
                Logs = new JobConfig.Logging
                {
                    Path = "abc",
                    Level = (int)LogLevel.Information,
                }
            })
            .Create();
        await _jobManager.CreateJobAsync(job);

        var jobWorker = CreateJobWorker(job, out var mockLoggerFactory, rootLogLevel: LogLevel.Information);
        await jobWorker.WorkAsync();

        var loggers = mockLoggerFactory.Loggers.ToList();
        loggers.Count.ShouldBe(2);
        var firstLogger = loggers[0];
        var secondLogger = loggers[1];

        secondLogger.Path.ShouldBe("abc");
        secondLogger.Logs.Count.ShouldBe(firstLogger.Logs.Count);
    }

    private IJobWorker CreateJobWorker(Job job, out LoggerFactoryMock mockLoggerFactory, LogLevel rootLogLevel = LogLevel.Debug)
    {
        mockLoggerFactory = new LoggerFactoryMock();
        var jobWorkerFactory = new JobWorkerFactory(
            GetRequiredService<IJobScopeService>(),
            GetRequiredService<ILoggerCollectionService>(),
            mockLoggerFactory)
        {
            Logger = mockLoggerFactory.CreateLogger("root", rootLogLevel),
        };

        return jobWorkerFactory.CreateWorker(job);
    }
}
