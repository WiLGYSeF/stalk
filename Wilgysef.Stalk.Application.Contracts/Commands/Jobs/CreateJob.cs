using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.Jobs;

public class CreateJob : ICommand
{
    public string? Name { get; }

    public int Priority { get; }


    public CreateJob(
        string? name,
        int priority = 0)
    {
        Name = name;
        Priority = priority;
    }
}
