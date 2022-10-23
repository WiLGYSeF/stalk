using Autofac;
using AutoMapper;
using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Application.Tests.Commands.Jobs;

public class UpdateJobTest : BaseTest
{
    private static readonly Type JobConfigType = typeof(JobConfig);
    private static readonly Type JobConfigDtoType = typeof(JobConfigDto);

    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<UpdateJob, JobDto> _updateJobCommandHandler;
    private readonly IBackgroundJobManager _backgroundJobManager;

    private readonly IMapper _mapper;

    public UpdateJobTest()
    {
        ReplaceSingletonService<IJobTaskWorkerFactory>(c => new JobTaskWorkerFactoryMock(
            c.Resolve<IServiceLocator>()));

        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();
        _updateJobCommandHandler = GetRequiredService<ICommandHandler<UpdateJob, JobDto>>();
        _backgroundJobManager = GetRequiredService<IBackgroundJobManager>();

        _mapper = GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Update_Job_Name()
    {
        var createCommand = new CreateJobBuilder(_mapper).WithRandom().Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        var command = new UpdateJob(
            jobId,
            name: RandomValues.RandomString(10));
        var job = await _updateJobCommandHandler.HandleCommandAsync(command);

        job.Name.ShouldBe(command.Name);
        job.Priority.ShouldBe(jobDto.Priority);
    }

    [Fact]
    public async Task Update_Job_Priority()
    {
        var createCommand = new CreateJobBuilder(_mapper).WithRandom().Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        var command = new UpdateJob(
            jobId,
            priority: RandomValues.RandomInt(100, 200));
        var job = await _updateJobCommandHandler.HandleCommandAsync(command);

        job.Name.ShouldBe(jobDto.Name);
        job.Priority.ShouldBe(command.Priority!.Value);

        var backgroundJobs = await _backgroundJobManager.GetJobsAsync();
        backgroundJobs.ShouldContain(j => j.JobArgsName == typeof(WorkPrioritizedJobsArgs).AssemblyQualifiedName);
    }

    [Theory]
    [InlineData(nameof(JobConfigDto.MaxTaskWorkerCount), 4, 10)]
    [InlineData(nameof(JobConfigDto.MaxTaskWorkerCount), 4, null)]
    [InlineData(nameof(JobConfigDto.DownloadFilenameTemplate), "a", "b")]
    [InlineData(nameof(JobConfigDto.DownloadFilenameTemplate), "a", null)]
    [InlineData(nameof(JobConfigDto.DownloadData), false, true)]
    [InlineData(nameof(JobConfigDto.DownloadData), false, null)]
    [InlineData(nameof(JobConfigDto.MetadataFilenameTemplate), "a", "b")]
    [InlineData(nameof(JobConfigDto.MetadataFilenameTemplate), "a", null)]
    [InlineData(nameof(JobConfigDto.SaveMetadata), false, true)]
    [InlineData(nameof(JobConfigDto.SaveMetadata), false, null)]
    [InlineData(nameof(JobConfigDto.ItemIdPath), "a", "b")]
    [InlineData(nameof(JobConfigDto.ItemIdPath), "a", null)]
    [InlineData(nameof(JobConfigDto.SaveItemIds), false, true)]
    [InlineData(nameof(JobConfigDto.SaveItemIds), false, null)]
    [InlineData(nameof(JobConfigDto.StopWithNoNewItemIds), false, true)]
    [InlineData(nameof(JobConfigDto.StopWithNoNewItemIds), false, null)]
    [InlineData(nameof(JobConfigDto.MaxFailures), 10, 100)]
    [InlineData(nameof(JobConfigDto.MaxFailures), 10, null)]
    public async Task Update_Job_Config_Basic(string name, object? oldValue, object? newValue)
    {
        var jobConfig = new JobConfig();
        JobConfigType.GetProperty(name)!.SetValue(jobConfig, oldValue);

        var createCommand = new CreateJobBuilder(_mapper)
            .WithRandom()
            .WithConfig(jobConfig)
            .Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        var jobConfigDto = new JobConfigDto();
        JobConfigDtoType.GetProperty(name)!.SetValue(jobConfigDto, newValue);

        var command = new UpdateJob(
            jobId,
            config: jobConfigDto);
        var job = await _updateJobCommandHandler.HandleCommandAsync(command);

        var actualValue = JobConfigDtoType.GetProperty(name)!.GetValue(job.Config);
        if (newValue != null)
        {
            actualValue.ShouldBe(newValue);
        }
        else
        {
            actualValue.ShouldBe(oldValue);
        }
    }

    [Theory]
    [InlineData("b", 2)]
    [InlineData("b", null)]
    [InlineData(null, 2)]
    public async Task Update_Job_Config_Logs(string? path, int? level)
    {
        var jobConfig = new JobConfig()
        {
            Logs = new JobConfig.Logging
            {
                Path = "a",
                Level = 1,
            }
        };

        var createCommand = new CreateJobBuilder(_mapper)
            .WithRandom()
            .WithConfig(jobConfig)
            .Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        var jobConfigDto = new JobConfigDto
        {
            Logs = new JobConfigDto.LoggingDto
            {
                Path = path,
                Level = level,
            }
        };

        var command = new UpdateJob(
            jobId,
            config: jobConfigDto);
        var job = await _updateJobCommandHandler.HandleCommandAsync(command);

        if (path != null)
        {
            job.Config.Logs!.Path.ShouldBe(path);
        }
        else
        {
            job.Config.Logs!.Path.ShouldBe(jobConfig.Logs.Path);
        }

        if (level.HasValue)
        {
            job.Config.Logs!.Level.ShouldBe(level);
        }
        else
        {
            job.Config.Logs!.Level.ShouldBe(jobConfig.Logs.Level);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_Job_Config_Delay(bool updateDelays)
    {
        var jobConfig = new JobConfig()
        {
            Delay = new JobConfig.DelayConfig
            {
                TaskDelay = new JobConfig.Range(0, 100),
                TaskFailedDelay = new JobConfig.Range(0, 100),
                TooManyRequestsDelay = new JobConfig.Range(0, 100),
                TaskWorkerDelay = new JobConfig.Range(0, 100),
            }
        };

        var createCommand = new CreateJobBuilder(_mapper)
            .WithRandom()
            .WithConfig(jobConfig)
            .Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        var jobConfigDto = new JobConfigDto
        {
            Delay = updateDelays ? new JobConfigDto.DelayConfigDto
            {
                TaskDelay = new JobConfigDto.RangeDto
                {
                    Min = 100,
                    Max = 200,
                },
                TaskFailedDelay = new JobConfigDto.RangeDto
                {
                    Min = 200,
                    Max = 300,
                },
                TooManyRequestsDelay = new JobConfigDto.RangeDto
                {
                    Min = 300,
                    Max = 400,
                },
                TaskWorkerDelay = new JobConfigDto.RangeDto
                {
                    Min = 400,
                    Max = 500
                }
            } : null
        };

        var command = new UpdateJob(
            jobId,
            config: jobConfigDto);
        var job = await _updateJobCommandHandler.HandleCommandAsync(command);

        if (updateDelays)
        {
            job.Config.Delay!.TaskDelay!.Min.ShouldBe(jobConfigDto.Delay!.TaskDelay!.Min);
            job.Config.Delay.TaskDelay!.Max.ShouldBe(jobConfigDto.Delay.TaskDelay!.Max);
            job.Config.Delay.TaskFailedDelay!.Min.ShouldBe(jobConfigDto.Delay.TaskFailedDelay!.Min);
            job.Config.Delay.TaskFailedDelay!.Min.ShouldBe(jobConfigDto.Delay.TaskFailedDelay!.Min);
            job.Config.Delay.TooManyRequestsDelay!.Min.ShouldBe(jobConfigDto.Delay.TooManyRequestsDelay!.Min);
            job.Config.Delay.TooManyRequestsDelay!.Max.ShouldBe(jobConfigDto.Delay.TooManyRequestsDelay!.Max);
            job.Config.Delay.TaskWorkerDelay!.Max.ShouldBe(jobConfigDto.Delay.TaskWorkerDelay!.Max);
            job.Config.Delay.TaskWorkerDelay!.Max.ShouldBe(jobConfigDto.Delay.TaskWorkerDelay!.Max);
        }
        else
        {
            job.Config.Delay!.TaskDelay!.Min.ShouldBe(jobConfig.Delay!.TaskDelay!.Min);
            job.Config.Delay.TaskDelay!.Max.ShouldBe(jobConfig.Delay.TaskDelay!.Max);
            job.Config.Delay.TaskFailedDelay!.Min.ShouldBe(jobConfig.Delay.TaskFailedDelay!.Min);
            job.Config.Delay.TaskFailedDelay!.Min.ShouldBe(jobConfig.Delay.TaskFailedDelay!.Min);
            job.Config.Delay.TooManyRequestsDelay!.Min.ShouldBe(jobConfig.Delay.TooManyRequestsDelay!.Min);
            job.Config.Delay.TooManyRequestsDelay!.Max.ShouldBe(jobConfig.Delay.TooManyRequestsDelay!.Max);
            job.Config.Delay.TaskWorkerDelay!.Max.ShouldBe(jobConfig.Delay.TaskWorkerDelay!.Max);
            job.Config.Delay.TaskWorkerDelay!.Max.ShouldBe(jobConfig.Delay.TaskWorkerDelay!.Max);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_Job_Config_ConfigGroup(bool updateConfigGroups)
    {
        var jobConfig = new JobConfig()
        {
            ExtractorConfig = new JobConfigGroupCollection(new[]
            {
                new JobConfigGroup
                {
                    Name = "a",
                    Config = new Dictionary<string, object?>
                    {
                        { "test", "abc" }
                    }
                }
            }),
            DownloaderConfig = new JobConfigGroupCollection(new[]
            {
                new JobConfigGroup
                {
                    Name = "b",
                    Config = new Dictionary<string, object?>
                    {
                        { "aaa", "123" }
                    }
                }
            })
        };

        var createCommand = new CreateJobBuilder(_mapper)
            .WithRandom()
            .WithConfig(jobConfig)
            .Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        var jobConfigDto = new JobConfigDto
        {
            ExtractorConfig = updateConfigGroups ? new[]
            {
                new JobConfigDto.ConfigGroupDto
                {
                    Name = "a",
                    Config = new Dictionary<string, object?>
                    {
                        { "testnew", "asdf" }
                    }
                }
            } : null,
            DownloaderConfig = updateConfigGroups ? new[]
            {
                new JobConfigDto.ConfigGroupDto
                {
                    Name = "d",
                    Config = new Dictionary<string, object?>
                    {
                        { "asdf", "123" }
                    }
                }
            } : null,
        };

        var command = new UpdateJob(
            jobId,
            config: jobConfigDto);
        var job = await _updateJobCommandHandler.HandleCommandAsync(command);

        if (updateConfigGroups)
        {
            job.Config.ExtractorConfig!.Count.ShouldBe(1);
            var extractorConfig = job.Config.ExtractorConfig.Single(c => c.Name == "a").Config;
            extractorConfig["test"]!.ToString().ShouldBe("abc");
            extractorConfig["testnew"]!.ToString().ShouldBe("asdf");

            job.Config.DownloaderConfig!.Count.ShouldBe(2);
            var downloaderConfigB = job.Config.DownloaderConfig.Single(c => c.Name == "b").Config;
            downloaderConfigB["aaa"]!.ToString().ShouldBe("123");

            var downloaderConfigD = job.Config.DownloaderConfig.Single(c => c.Name == "d").Config;
            downloaderConfigD["asdf"]!.ToString().ShouldBe("123");
        }
        else
        {
            job.Config.ExtractorConfig!.Count.ShouldBe(1);
            var extractorConfig = job.Config.ExtractorConfig.Single(c => c.Name == "a").Config;
            extractorConfig["test"]!.ToString().ShouldBe("abc");

            job.Config.DownloaderConfig!.Count.ShouldBe(1);
            var downloaderConfig = job.Config.DownloaderConfig.Single(c => c.Name == "b").Config;
            downloaderConfig["aaa"]!.ToString().ShouldBe("123");
        }
    }
}
