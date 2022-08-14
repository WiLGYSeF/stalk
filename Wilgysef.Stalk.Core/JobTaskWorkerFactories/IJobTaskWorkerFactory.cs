﻿using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobTaskWorkerFactories;

public interface IJobTaskWorkerFactory : ITransientDependency
{
    JobTaskWorker CreateWorker(JobTask task);
}
