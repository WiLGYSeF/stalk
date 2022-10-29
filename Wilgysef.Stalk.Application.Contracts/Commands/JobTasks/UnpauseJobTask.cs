using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;

public class UnpauseJobTask : ICommand
{
    public long Id { get; }

    public UnpauseJobTask(long id)
    {
        Id = id;
    }
}
