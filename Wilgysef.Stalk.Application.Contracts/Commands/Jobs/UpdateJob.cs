using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.Jobs;

public class UpdateJob : ICommand
{
    public long Id { get; }

    public string? Name { get; set; }

    public int? Priority { get; set; }

    public JobConfigDto? Config { get; set; }

    public UpdateJob(
        long id,
        string? name = null,
        int? priority = null,
        JobConfigDto? config = null)
    {
        Id = id;
        Name = name;
        Priority = priority;
        Config = config;
    }
}
