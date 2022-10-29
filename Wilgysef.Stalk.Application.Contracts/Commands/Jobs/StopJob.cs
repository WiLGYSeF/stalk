using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.Jobs;

public class StopJob : ICommand
{
    public long Id { get; }

    public StopJob(long id)
    {
        Id = id;
    }
}
