using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;

public class StopJobTask : ICommand
{
    public long Id { get; }

    public StopJobTask(long id)
    {
        Id = id;
    }
}
