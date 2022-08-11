using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;

public class PauseJobTask : ICommand
{
    public long Id { get; }

    public PauseJobTask(long id)
    {
        Id = id;
    }
}
