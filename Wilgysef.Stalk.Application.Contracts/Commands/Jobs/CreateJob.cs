using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.Jobs;

public class CreateJob : ICommand
{
    public string? Name { get; }

    public int Priority { get; }

    public DateTime? DelayedUntil { get; }

    public JobConfigDto Config { get; }

    public ICollection<CreateJobTaskDto> Tasks { get; }

    public CreateJob(
        string? name,
        JobConfigDto config,
        ICollection<CreateJobTaskDto> tasks,
        int priority = 0,
        DateTime? delayedUntil = null)
    {
        Name = name;
        Config = config;
        Tasks = tasks;
        Priority = priority;
        DelayedUntil = delayedUntil;
    }
}
