using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.Jobs;

public class CreateJob : ICommand
{
    public string? Name { get; set; }

    public CreateJob(string? name)
    {
        Name = name;
    }
}
