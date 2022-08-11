using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;

public class DeleteJobTask : ICommand
{
    public long Id { get; }

    public DeleteJobTask(long id)
    {
        Id = id;
    }
}
