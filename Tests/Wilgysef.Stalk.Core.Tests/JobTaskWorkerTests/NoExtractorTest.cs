﻿using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Tests.Utilities;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;

namespace Wilgysef.Stalk.Core.Tests.JobTaskWorkerTests;

public class NoExtractorTest : BaseTest
{
    private readonly Mock<IExtractor> _extractorMock;

    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public NoExtractorTest()
    {
        _extractorMock = new Mock<IExtractor>();
        _extractorMock.Setup(m => m.CanExtract(It.IsAny<Uri>())).Returns(false);

        ReplaceService(_ => _extractorMock.Object);

        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        _jobWorkerStarter = new JobWorkerStarter(_jobWorkerFactory);
    }

    [Fact]
    public async Task Work_Extract_No_Extractor()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(job.Id, job => job.State == JobState.Active);
        job = await this.WaitUntilJobAsync(job.Id, job => job.IsDone);

        var jobTask = job.Tasks.Single();
        jobTask.Result.Success!.Value.ShouldBeFalse();
        jobTask.Result.ErrorCode.ShouldBe(StalkErrorCodes.JobTaskWorkerNoExtractor);
    }
}
