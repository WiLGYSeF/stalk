using AutoMapper;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Application.Tests;

internal class CreateJobBuilder
{
    public string? Name { get; set; }

    public int Priority { get; set; }

    public DateTime? DelayedUntil { get; set; }

    public JobConfig Config { get; set; } = new();

    public List<CreateJobTaskDto> Tasks { get; set; } = new();

    private readonly IMapper _mapper;

    public CreateJobBuilder(IMapper mapper)
    {
        _mapper = mapper;
    }

    public CreateJob Create()
    {
        return new CreateJob(
            Name,
            _mapper.Map<JobConfigDto>(Config),
            Tasks,
            Priority,
            DelayedUntil);
    }

    public CreateJobBuilder WithRandom()
    {
        Name = RandomValues.RandomString(8);
        Priority = RandomValues.RandomInt(-100, 100);
        WithRandomTasks(5);
        return this;
    }

    public CreateJobBuilder WithName(string? name)
    {
        Name = name;
        return this;
    }

    public CreateJobBuilder WithPriority(int priority)
    {
        Priority = priority;
        return this;
    }

    public CreateJobBuilder WithDelayedUntil(DateTime? delayedUntil)
    {
        DelayedUntil = delayedUntil;
        return this;
    }

    public CreateJobBuilder WithConfig(JobConfig config)
    {
        Config = config;
        return this;
    }

    public CreateJobBuilder WithTasks(params CreateJobTaskDto[] tasks)
    {
        Tasks.AddRange(tasks);
        return this;
    }

    public CreateJobBuilder WithNoTasks()
    {
        Tasks.Clear();
        return this;
    }

    public CreateJobBuilder WithRandomTasks(int count)
    {
        var tasks = new List<CreateJobTaskDto>();

        for (var i = 0; i < count; i++)
        {
            tasks.Add(new CreateJobTaskDto
            {
                Name = RandomValues.RandomString(6),
                Priority = RandomValues.RandomInt(-100, 100),
                Uri = RandomValues.RandomUri().AbsoluteUri,
                DelayedUntil = null,
            });
        }

        Tasks.AddRange(tasks);
        return this;
    }
}
