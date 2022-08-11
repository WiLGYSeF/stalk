using IdGen;
using Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Commands;

public class JobTaskCommandHandler : CommandQuery,
    ICommandHandler<CreateJobTask, JobDto>
{
    private readonly IJobManager _jobManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobTaskCommandHandler(
        IJobManager jobManager,
        IIdGenerator<long> idGenerator)
    {
        _jobManager = jobManager;
        _idGenerator = idGenerator;
    }

    public async Task<JobDto> HandleCommandAsync(CreateJobTask command)
    {
        var job = await _jobManager.GetJobAsync(command.JobId);

        var builder = new JobTaskBuilder()
            .WithId(_idGenerator.CreateId())
            .WithName(command.Name)
            .WithPriority(command.Priority)
            .WithUri(command.Uri)
            .WithDelayedUntilTime(command.DelayedUntil);

        job.AddTask(builder.Create());

        await _jobManager.UpdateJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }
}
