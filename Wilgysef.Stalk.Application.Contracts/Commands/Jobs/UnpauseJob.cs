using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.Jobs;

public class UnpauseJob : ICommand
{
    public long Id { get; set; }

    public UnpauseJob(long id)
    {
        Id = id;
    }
}
