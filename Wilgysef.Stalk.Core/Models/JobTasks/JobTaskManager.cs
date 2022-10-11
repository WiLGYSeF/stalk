using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.Core.Specifications;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTaskManager : IJobTaskManager, ITransientDependency
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobTaskRepository _jobTaskRepository;

    public JobTaskManager(
        IUnitOfWork unitOfWork,
        IJobTaskRepository jobTaskRepository)
    {
        _unitOfWork = unitOfWork;
        _jobTaskRepository = jobTaskRepository;
    }

    public async Task<JobTask> CreateJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default)
    {
        var entity = await _jobTaskRepository.AddAsync(jobTask, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task CreateJobTasksAsync(IEnumerable<JobTask> jobTasks, CancellationToken cancellationToken = default)
    {
        await _jobTaskRepository.AddRangeAsync(jobTasks, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobTask> GetJobTaskAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _jobTaskRepository.FirstOrDefaultAsync(
            new JobTaskSingleSpecification(jobTaskId: id),
            cancellationToken);
        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(JobTask), id);
        }

        return entity;
    }

    public async Task<JobTask> UpdateJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default)
    {
        _jobTaskRepository.Update(jobTask);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return jobTask;
    }

    public async Task DeleteJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default)
    {
        if (jobTask.IsActive)
        {
            throw new JobTaskActiveException();
        }

        _jobTaskRepository.Remove(jobTask);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetJobTaskActiveAsync(JobTask jobTask, CancellationToken cancellationToken = default)
    {
        if (jobTask.IsActive)
        {
            return;
        }

        jobTask.ChangeState(JobTaskState.Active);

        await UpdateJobTaskAsync(jobTask, cancellationToken);
    }
}
