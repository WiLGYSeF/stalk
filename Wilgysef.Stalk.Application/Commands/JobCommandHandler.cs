using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.IdGenerators;

namespace Wilgysef.Stalk.Application.Commands;

public class JobCommandHandler : Command,
    ICommandHandler<CreateJob, JobDto>,
    ICommandHandler<UpdateJob, JobDto>,
    ICommandHandler<StopJob, JobDto>,
    ICommandHandler<DeleteJob, JobDto>,
    ICommandHandler<PauseJob, JobDto>,
    ICommandHandler<UnpauseJob, JobDto>
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobCommandHandler(
        IJobManager jobManager,
        IJobStateManager jobStateManager,
        IIdGenerator<long> idGenerator)
    {
        _jobManager = jobManager;
        _jobStateManager = jobStateManager;
        _idGenerator = idGenerator;
    }

    public async Task<JobDto> HandleCommandAsync(CreateJob command)
    {
        var config = Mapper.Map<JobConfig>(command.Config);

        var builder = new JobBuilder().WithId(_idGenerator.CreateId())
            .WithName(command.Name)
            .WithPriority(command.Priority)
            .WithDelayedUntilTime(command.DelayedUntil)
            .WithConfig(config);

        foreach (var task in command.Tasks)
        {
            var taskBuilder = new JobTaskBuilder().WithId(_idGenerator.CreateId())
                .WithName(task.Name)
                .WithPriority(task.Priority)
                .WithUri(task.Uri)
                .WithDelayedUntilTime(task.DelayedUntil);
            builder.WithTasks(taskBuilder.Create());
        }

        var job = await _jobManager.CreateJobAsync(builder.Create());
        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(UpdateJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        if (command.Name != null)
        {
            job.ChangeName(command.Name);
        }
        if (command.Priority.HasValue)
        {
            job.ChangePriority(command.Priority.Value);
        }
        if (command.Config != null)
        {
            UpdateConfig(job, command.Config);
        }

        job = await _jobManager.UpdateJobAsync(job);
        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(StopJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        await _jobStateManager.StopJobAsync(job);
        job = await _jobManager.GetJobAsync(job.Id);
        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(DeleteJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        await _jobStateManager.StopJobAsync(job);
        job = await _jobManager.GetJobAsync(job.Id);

        await _jobManager.DeleteJobAsync(job);
        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(PauseJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        await _jobStateManager.PauseJobAsync(job);
        job = await _jobManager.GetJobAsync(job.Id);
        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(UnpauseJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        await _jobStateManager.UnpauseJobAsync(job);
        job = await _jobManager.GetJobAsync(job.Id);
        return Mapper.Map<JobDto>(job);
    }

    private void UpdateConfig(Job job, JobConfigDto config)
    {
        var jobConfig = job.GetConfig();

        SetIfNotNull(v => jobConfig.MaxTaskWorkerCount = v!.Value, () => config.MaxTaskWorkerCount);
        SetIfNotNull(v => jobConfig.DownloadFilenameTemplate = v, () => config.DownloadFilenameTemplate);
        SetIfNotNull(v => jobConfig.DownloadData = v!.Value, () => config.DownloadData);
        SetIfNotNull(v => jobConfig.MetadataFilenameTemplate = v, () => config.MetadataFilenameTemplate);
        SetIfNotNull(v => jobConfig.SaveMetadata = v!.Value, () => config.SaveMetadata);
        SetIfNotNull(v => jobConfig.ItemIdPath = v, () => config.ItemIdPath);
        SetIfNotNull(v => jobConfig.SaveItemIds = v!.Value, () => config.SaveItemIds);
        SetIfNotNull(v => jobConfig.StopWithNoNewItemIds = v!.Value, () => config.StopWithNoNewItemIds);
        SetIfNotNull(v => jobConfig.MaxFailures = v, () => config.MaxFailures);

        if (config.Logs != null)
        {
            jobConfig.Logs ??= new();
            SetIfNotNull(v => jobConfig.Logs!.Path = v, () => config.Logs.Path);
            SetIfNotNull(v => jobConfig.Logs!.Level = v!.Value, () => config.Logs.Level);
        }

        if (config.Delay != null)
        {
            if (config.Delay.TaskDelay != null)
            {
                jobConfig.Delay.TaskDelay = new(config.Delay.TaskDelay.Min, config.Delay.TaskDelay.Max);
            }
            if (config.Delay.TaskFailedDelay != null)
            {
                jobConfig.Delay.TaskFailedDelay = new(config.Delay.TaskFailedDelay.Min, config.Delay.TaskFailedDelay.Max);
            }
            if (config.Delay.TooManyRequestsDelay != null)
            {
                jobConfig.Delay.TooManyRequestsDelay = new(config.Delay.TooManyRequestsDelay.Min, config.Delay.TooManyRequestsDelay.Max);
            }
        }

        if (config.ExtractorConfig != null)
        {
            foreach (var group in config.ExtractorConfig)
            {
                jobConfig.ExtractorConfig.Add(Mapper.Map<JobConfigGroup>(group));
            }
        }
        if (config.DownloaderConfig != null)
        {
            foreach (var group in config.DownloaderConfig)
            {
                jobConfig.DownloaderConfig.Add(Mapper.Map<JobConfigGroup>(group));
            }
        }

        job.ChangeConfig(jobConfig);
    }

    private void SetIfNotNull<T>(Action<T> action, Func<T> valueFunc)
    {
        var value = valueFunc();
        if (value != null)
        {
            action(value);
        }
    }
}
