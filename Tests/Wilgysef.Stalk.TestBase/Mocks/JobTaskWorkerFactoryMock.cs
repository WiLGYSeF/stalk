using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobTaskWorkerFactoryMock : IJobTaskWorkerFactory
{
    public event EventHandler<WorkEventArgs> WorkEvent;

    private readonly IServiceLocator _serviceLocator;

    public JobTaskWorkerFactoryMock(IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobTaskWorker CreateWorker(Job job, JobTask jobTask)
    {
        var worker = new JobTaskWorkerMock(_serviceLocator);
        worker.WithJobTask(job, jobTask);
        worker.WorkEvent += (sender, args) => OnWorkEvent(worker);
        return worker;
    }

    private void OnWorkEvent(JobTaskWorkerMock jobTaskWorker)
    {
        WorkEvent(jobTaskWorker, new WorkEventArgs(jobTaskWorker));
    }

    public class WorkEventArgs : EventArgs
    {
        public JobTaskWorkerMock JobTaskWorker { get; }

        public WorkEventArgs(JobTaskWorkerMock jobTaskWorker)
        {
            JobTaskWorker = jobTaskWorker;
        }
    }
}
