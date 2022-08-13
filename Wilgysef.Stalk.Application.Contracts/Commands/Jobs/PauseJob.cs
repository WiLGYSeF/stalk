using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.Jobs;

public class PauseJob : ICommand
{
    public long Id { get; set; }

    public PauseJob(long id)
    {
        Id = id;
    }
}
