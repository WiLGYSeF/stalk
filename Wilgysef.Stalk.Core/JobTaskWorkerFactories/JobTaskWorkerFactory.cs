﻿using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobTaskWorkerFactories;

public class JobTaskWorkerFactory : IJobTaskWorkerFactory
{
    private readonly IServiceLocator _serviceLocator;

    public JobTaskWorkerFactory(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public JobTaskWorker CreateWorker(JobTask task)
    {
        var taskWorker = new JobTaskWorker(_serviceLocator);
        taskWorker.WithJobTask(task);
        return taskWorker;
    }
}
