using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.Jobs;

public class DeleteJob : ICommand
{
    public long Id { get; }

    public DeleteJob(long id)
    {
        Id = id;
    }
}
