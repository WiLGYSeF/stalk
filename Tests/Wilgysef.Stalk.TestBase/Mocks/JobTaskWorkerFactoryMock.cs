using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobTaskWorkerFactoryMock : IJobTaskWorkerFactory
{
    public event EventHandler<WorkEventArgs> WorkEvent;

    private readonly List<IJobTaskWorker> _jobTaskWorkers = new();

    private readonly IServiceLocator _serviceLocator;

    public JobTaskWorkerFactoryMock(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobTaskWorker CreateWorker(JobTask jobTask)
    {
        var worker = new JobTaskWorkerMock(_serviceLocator.BeginLifetimeScopeFromRoot());
        worker.WithJobTask(jobTask);
        worker.WorkEvent += (sender, args) => OnWorkEvent(worker);
        _jobTaskWorkers.Add(worker);
        return worker;
    }

    public void FinishJobTaskWorker(JobTask jobTask)
    {
        var worker = GetJobTaskWorkerMock(jobTask);
        worker.Finish();
        _jobTaskWorkers.Remove(worker);
    }

    public void FailJobTaskWorker(JobTask jobTask)
    {
        var worker = GetJobTaskWorkerMock(jobTask);
        worker.Fail();
        _jobTaskWorkers.Remove(worker);
    }

    private JobTaskWorkerMock GetJobTaskWorkerMock(JobTask jobTask)
    {
        var worker = _jobTaskWorkers.Single(w => w.JobTask!.Id == jobTask.Id) as JobTaskWorkerMock;
        if (worker == null)
        {
            throw new ArgumentException("Job task does not have a worker", nameof(jobTask));
        }
        return worker;
    }

    private void OnWorkEvent(JobTaskWorkerMock jobTaskWorker)
    {
        WorkEvent?.Invoke(jobTaskWorker, new WorkEventArgs(jobTaskWorker));
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
